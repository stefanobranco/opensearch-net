using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace OpenSearch.Net;

/// <summary>
/// The primary transport implementation that sends HTTP requests to an OpenSearch cluster.
/// Handles node selection, retries with dead-node tracking, authentication, compression,
/// and diagnostic auditing. Uses <see cref="HttpClient"/> with handler rotation for DNS refresh.
/// </summary>
public sealed class HttpClientTransport : IOpenSearchTransport, IDisposable
{
	private static readonly HashSet<int> RetryableStatusCodes = [502, 503, 504];

	private readonly ITransportConfiguration _configuration;
	private readonly HttpClientFactory _httpClientFactory;
	private readonly IOpenSearchSerializer _serializer;

	/// <inheritdoc />
	public IOpenSearchSerializer Serializer => _serializer;

	/// <inheritdoc />
	public TransportOptions? DefaultOptions { get; }

	/// <summary>
	/// Creates a new transport with the given configuration.
	/// </summary>
	public HttpClientTransport(ITransportConfiguration configuration, TransportOptions? defaultOptions = null)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		_configuration = configuration;
		_serializer = configuration.Serializer ?? new DefaultJsonSerializer();
		DefaultOptions = defaultOptions;
		_httpClientFactory = new HttpClientFactory(
			configuration.DnsRefreshTimeout,
			() => CreateHandler(configuration));
	}

	/// <inheritdoc />
	public TResponse PerformRequest<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null)
	{
		var auditTrail = new RequestAuditTrail();
		var maxRetries = _configuration.MaxRetries;
		var nodePool = _configuration.NodePool;
		var mergedOptions = MergeOptions(options);

		Exception? lastException = null;

		for (var attempt = 0; attempt <= maxRetries; attempt++)
		{
			if (attempt > 0)
				auditTrail.Add(AuditEventType.Retry);

			var node = nodePool.SelectNode();
			auditTrail.Add(AuditEventType.NodeSelected, node);

			var sw = Stopwatch.StartNew();
			HttpClient? client = null;

			try
			{
				client = _httpClientFactory.CreateClient();
				client.Timeout = _configuration.RequestTimeout;

				using var requestMessage = BuildRequestMessage(request, endpoint, node, mergedOptions);
				auditTrail.Add(AuditEventType.RequestSent, node);

				using var responseMessage = client.Send(requestMessage, HttpCompletionOption.ResponseHeadersRead);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				auditTrail.Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode) && attempt < maxRetries)
				{
					auditTrail.Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					auditTrail.Add(AuditEventType.DeadNode, node);
					lastException = new TransportException(
						$"Received retryable status code {statusCode} from {node.Host}",
						statusCode, null, node);
					continue;
				}

				nodePool.MarkAlive(node);

				using var bodyStream = responseMessage.Content.ReadAsStream();
				var contentType = responseMessage.Content.Headers.ContentType?.MediaType;

				return endpoint.DeserializeResponse(statusCode, contentType, bodyStream, _serializer);
			}
			catch (HttpRequestException ex)
			{
				sw.Stop();
				auditTrail.Add(AuditEventType.BadResponse, node, exception: ex, duration: sw.Elapsed);
				nodePool.MarkDead(node);
				auditTrail.Add(AuditEventType.DeadNode, node);
				lastException = ex;

				if (attempt >= maxRetries)
					break;
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				sw.Stop();
				auditTrail.Add(AuditEventType.BadResponse, node, exception: ex, duration: sw.Elapsed);
				nodePool.MarkDead(node);
				auditTrail.Add(AuditEventType.DeadNode, node);
				lastException = ex;

				if (attempt >= maxRetries)
					break;
			}
			finally
			{
				client?.Dispose();
			}
		}

		auditTrail.Add(AuditEventType.AllNodesDead);
		throw new TransportException(
			"Maximum number of retries exhausted. No healthy nodes available.",
			lastException ?? new InvalidOperationException("No attempts were made."))
		{
			AuditTrail = auditTrail
		};
	}

	/// <inheritdoc />
	public async Task<TResponse> PerformRequestAsync<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null,
		CancellationToken ct = default)
	{
		var auditTrail = new RequestAuditTrail();
		var maxRetries = _configuration.MaxRetries;
		var nodePool = _configuration.NodePool;
		var mergedOptions = MergeOptions(options);

		Exception? lastException = null;

		for (var attempt = 0; attempt <= maxRetries; attempt++)
		{
			if (attempt > 0)
				auditTrail.Add(AuditEventType.Retry);

			var node = nodePool.SelectNode();
			auditTrail.Add(AuditEventType.NodeSelected, node);

			var sw = Stopwatch.StartNew();
			HttpClient? client = null;

			try
			{
				client = _httpClientFactory.CreateClient();
				client.Timeout = _configuration.RequestTimeout;

				using var requestMessage = await BuildRequestMessageAsync(request, endpoint, node, mergedOptions, ct)
					.ConfigureAwait(false);
				auditTrail.Add(AuditEventType.RequestSent, node);

				using var responseMessage = await client
					.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct)
					.ConfigureAwait(false);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				auditTrail.Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode) && attempt < maxRetries)
				{
					auditTrail.Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					auditTrail.Add(AuditEventType.DeadNode, node);
					lastException = new TransportException(
						$"Received retryable status code {statusCode} from {node.Host}",
						statusCode, null, node);
					continue;
				}

				nodePool.MarkAlive(node);

				var bodyStream = await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
				var contentType = responseMessage.Content.Headers.ContentType?.MediaType;

				return endpoint.DeserializeResponse(statusCode, contentType, bodyStream, _serializer);
			}
			catch (HttpRequestException ex)
			{
				sw.Stop();
				auditTrail.Add(AuditEventType.BadResponse, node, exception: ex, duration: sw.Elapsed);
				nodePool.MarkDead(node);
				auditTrail.Add(AuditEventType.DeadNode, node);
				lastException = ex;

				if (attempt >= maxRetries)
					break;
			}
			catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
			{
				// Timeout, not user cancellation.
				sw.Stop();
				auditTrail.Add(AuditEventType.BadResponse, node, exception: ex, duration: sw.Elapsed);
				nodePool.MarkDead(node);
				auditTrail.Add(AuditEventType.DeadNode, node);
				lastException = ex;

				if (attempt >= maxRetries)
					break;
			}
			finally
			{
				client?.Dispose();
			}
		}

		auditTrail.Add(AuditEventType.AllNodesDead);
		throw new TransportException(
			"Maximum number of retries exhausted. No healthy nodes available.",
			lastException ?? new InvalidOperationException("No attempts were made."))
		{
			AuditTrail = auditTrail
		};
	}

	private HttpRequestMessage BuildRequestMessage<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		Node node,
		TransportOptions? options)
	{
		var method = MapHttpMethod(endpoint.Method(request));
		var uri = BuildRequestUri(node, endpoint.RequestUrl(request), options);

		var message = new HttpRequestMessage(method, uri);
		ApplyHeaders(message, options);
		ApplyAuthentication(message);

		var body = endpoint.GetBody(request);
		if (body is not null)
		{
			var stream = new MemoryStream();
			body.WriteTo(stream, _serializer);
			stream.Position = 0;

			var content = new StreamContent(stream);
			content.Headers.ContentType = new MediaTypeHeaderValue(body.ContentType);

			if (_configuration.EnableHttpCompression)
				message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

			message.Content = content;
		}
		else if (_configuration.EnableHttpCompression)
		{
			message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
		}

		_configuration.OnRequestCreated?.Invoke(message);

		return message;
	}

	private async ValueTask<HttpRequestMessage> BuildRequestMessageAsync<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		Node node,
		TransportOptions? options,
		CancellationToken ct)
	{
		var method = MapHttpMethod(endpoint.Method(request));
		var uri = BuildRequestUri(node, endpoint.RequestUrl(request), options);

		var message = new HttpRequestMessage(method, uri);
		ApplyHeaders(message, options);
		ApplyAuthentication(message);

		var body = endpoint.GetBody(request);
		if (body is not null)
		{
			var stream = new MemoryStream();
			await body.WriteToAsync(stream, _serializer, ct).ConfigureAwait(false);
			stream.Position = 0;

			var content = new StreamContent(stream);
			content.Headers.ContentType = new MediaTypeHeaderValue(body.ContentType);

			if (_configuration.EnableHttpCompression)
				message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

			message.Content = content;
		}
		else if (_configuration.EnableHttpCompression)
		{
			message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
		}

		_configuration.OnRequestCreated?.Invoke(message);

		return message;
	}

	private static Uri BuildRequestUri(Node node, string requestUrl, TransportOptions? options)
	{
		var baseUri = node.Host;
		var path = requestUrl.StartsWith('/') ? requestUrl : "/" + requestUrl;

		if (options?.QueryParameters is { Count: > 0 } queryParams)
		{
			var sb = new StringBuilder(path);
			sb.Append('?');
			var first = true;
			foreach (var (key, value) in queryParams)
			{
				if (!first) sb.Append('&');
				sb.Append(Uri.EscapeDataString(key));
				sb.Append('=');
				sb.Append(Uri.EscapeDataString(value));
				first = false;
			}
			path = sb.ToString();
		}

		return new Uri(baseUri, path);
	}

	private void ApplyHeaders(HttpRequestMessage message, TransportOptions? options)
	{
		message.Headers.Add("Accept", "application/json");

		if (DefaultOptions?.Headers is { Count: > 0 } defaultHeaders)
		{
			foreach (var (key, value) in defaultHeaders)
				message.Headers.TryAddWithoutValidation(key, value);
		}

		if (options?.Headers is { Count: > 0 } headers)
		{
			foreach (var (key, value) in headers)
				message.Headers.TryAddWithoutValidation(key, value);
		}
	}

	private void ApplyAuthentication(HttpRequestMessage message)
	{
		if (_configuration.BasicAuth is { } basic)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basic.Username}:{basic.Password}"));
			message.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
		}
		else if (_configuration.ApiKeyAuth is { } apiKey)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey.Id}:{apiKey.ApiKey}"));
			message.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", encoded);
		}
	}

	private TransportOptions? MergeOptions(TransportOptions? perRequest)
	{
		if (perRequest is null)
			return DefaultOptions;

		if (DefaultOptions is null)
			return perRequest;

		// Merge: per-request headers override defaults, query params are combined.
		Dictionary<string, string>? mergedHeaders = null;
		if (DefaultOptions.Headers is not null || perRequest.Headers is not null)
		{
			mergedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (DefaultOptions.Headers is not null)
			{
				foreach (var (key, value) in DefaultOptions.Headers)
					mergedHeaders[key] = value;
			}
			if (perRequest.Headers is not null)
			{
				foreach (var (key, value) in perRequest.Headers)
					mergedHeaders[key] = value;
			}
		}

		Dictionary<string, string>? mergedQuery = null;
		if (DefaultOptions.QueryParameters is not null || perRequest.QueryParameters is not null)
		{
			mergedQuery = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (DefaultOptions.QueryParameters is not null)
			{
				foreach (var (key, value) in DefaultOptions.QueryParameters)
					mergedQuery[key] = value;
			}
			if (perRequest.QueryParameters is not null)
			{
				foreach (var (key, value) in perRequest.QueryParameters)
					mergedQuery[key] = value;
			}
		}

		return new TransportOptions
		{
			Headers = mergedHeaders,
			QueryParameters = mergedQuery,
			WarningsHandler = perRequest.WarningsHandler ?? DefaultOptions.WarningsHandler
		};
	}

	private static void ProcessWarningHeaders(HttpResponseMessage response, TransportOptions? options)
	{
		if (options?.WarningsHandler is not { } handler)
			return;

		if (!response.Headers.TryGetValues("Warning", out var warnings))
			return;

		foreach (var warning in warnings)
			handler(warning);
	}

	private static System.Net.Http.HttpMethod MapHttpMethod(HttpMethod method) =>
		method switch
		{
			HttpMethod.Get => System.Net.Http.HttpMethod.Get,
			HttpMethod.Post => System.Net.Http.HttpMethod.Post,
			HttpMethod.Put => System.Net.Http.HttpMethod.Put,
			HttpMethod.Delete => System.Net.Http.HttpMethod.Delete,
			HttpMethod.Head => System.Net.Http.HttpMethod.Head,
			HttpMethod.Patch => System.Net.Http.HttpMethod.Patch,
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported HTTP method.")
		};

	private static HttpMessageHandler CreateHandler(ITransportConfiguration configuration)
	{
		var handler = new SocketsHttpHandler
		{
			PooledConnectionLifetime = configuration.DnsRefreshTimeout,
			AutomaticDecompression = configuration.EnableHttpCompression
				? DecompressionMethods.GZip | DecompressionMethods.Deflate
				: DecompressionMethods.None
		};

		if (configuration.DisableAutomaticProxyDetection)
		{
			handler.UseProxy = false;
		}
		else if (configuration.ProxyAddress is not null)
		{
			var proxy = new WebProxy(configuration.ProxyAddress);
			if (configuration.ProxyUsername is not null)
				proxy.Credentials = new NetworkCredential(configuration.ProxyUsername, configuration.ProxyPassword);
			handler.Proxy = proxy;
			handler.UseProxy = true;
		}

		return handler;
	}

	/// <inheritdoc />
	public void Dispose() => _httpClientFactory.Dispose();
}
