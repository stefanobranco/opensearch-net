using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenSearch.Net;

/// <summary>
/// The primary transport implementation that sends HTTP requests to an OpenSearch cluster.
/// Handles node selection, retries with dead-node tracking, authentication, compression,
/// and diagnostic auditing. Uses <see cref="HttpClient"/> with handler rotation for DNS refresh.
/// </summary>
public sealed class HttpClientTransport : IOpenSearchTransport, IDisposable
{
	private static readonly HashSet<int> RetryableStatusCodes = [502, 503, 504];
	private static readonly StringWithQualityHeaderValue GzipEncoding = new("gzip");

	private readonly ITransportConfiguration _configuration;
	private readonly HttpClientFactory _httpClientFactory;
	private readonly IOpenSearchSerializer _serializer;
	private readonly AuthenticationHeaderValue? _cachedAuthHeader;

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
			() => CreateHandler(configuration),
			configuration.RequestTimeout);
		_cachedAuthHeader = BuildAuthHeader(configuration);
	}

	/// <inheritdoc />
	public TResponse PerformRequest<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null)
	{
		var maxRetries = _configuration.MaxRetries;
		var nodePool = _configuration.NodePool;
		var mergedOptions = MergeOptions(options);
		RequestAuditTrail? auditTrail = null;

		Exception? lastException = null;
		int lastRetryableStatusCode = 0;
		Node? lastRetryableNode = null;

		for (var attempt = 0; attempt <= maxRetries; attempt++)
		{
			if (attempt > 0)
				GetAuditTrail(ref auditTrail).Add(AuditEventType.Retry);

			var node = nodePool.SelectNode();
			GetAuditTrail(ref auditTrail).Add(AuditEventType.NodeSelected, node);

			var sw = Stopwatch.StartNew();

			try
			{
				var client = _httpClientFactory.GetClient();

				using var requestMessage = BuildRequestMessage(request, endpoint, node, mergedOptions);
				GetAuditTrail(ref auditTrail).Add(AuditEventType.RequestSent, node);

				using var responseMessage = client.Send(requestMessage, HttpCompletionOption.ResponseHeadersRead);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				GetAuditTrail(ref auditTrail).Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode) && attempt < maxRetries)
				{
					GetAuditTrail(ref auditTrail).Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					GetAuditTrail(ref auditTrail).Add(AuditEventType.DeadNode, node);
					lastRetryableStatusCode = statusCode;
					lastRetryableNode = node;
					lastException = null;
					continue;
				}

				nodePool.MarkAlive(node);

				var method = endpoint.Method(request);

				if (IsServerError(statusCode, method))
				{
					using var errorStream = responseMessage.Content.ReadAsStream();
					ThrowServerError(statusCode, errorStream, node);
				}

				using var bodyStream = responseMessage.Content.ReadAsStream();
				var contentType = responseMessage.Content.Headers.ContentType?.MediaType;

				return endpoint.DeserializeResponse(statusCode, contentType, bodyStream, _serializer);
			}
			catch (Exception ex) when (IsRetryableException(ex, isSync: true))
			{
				sw.Stop();
				HandleRetryableException(ref auditTrail, nodePool, node, ex, sw.Elapsed);
				lastException = ex;
				lastRetryableStatusCode = 0;
				lastRetryableNode = null;

				if (attempt >= maxRetries)
					break;
			}
		}

		return ThrowRetriesExhausted<TResponse>(ref auditTrail, lastException, lastRetryableStatusCode, lastRetryableNode);
	}

	/// <inheritdoc />
	public async Task<TResponse> PerformRequestAsync<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null,
		CancellationToken ct = default)
	{
		var maxRetries = _configuration.MaxRetries;
		var nodePool = _configuration.NodePool;
		var mergedOptions = MergeOptions(options);
		RequestAuditTrail? auditTrail = null;

		Exception? lastException = null;
		int lastRetryableStatusCode = 0;
		Node? lastRetryableNode = null;

		for (var attempt = 0; attempt <= maxRetries; attempt++)
		{
			if (attempt > 0)
				GetAuditTrail(ref auditTrail).Add(AuditEventType.Retry);

			var node = nodePool.SelectNode();
			GetAuditTrail(ref auditTrail).Add(AuditEventType.NodeSelected, node);

			var sw = Stopwatch.StartNew();

			try
			{
				var client = _httpClientFactory.GetClient();

				using var requestMessage = await BuildRequestMessageAsync(request, endpoint, node, mergedOptions, ct)
					.ConfigureAwait(false);
				GetAuditTrail(ref auditTrail).Add(AuditEventType.RequestSent, node);

				using var responseMessage = await client
					.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct)
					.ConfigureAwait(false);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				GetAuditTrail(ref auditTrail).Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode) && attempt < maxRetries)
				{
					GetAuditTrail(ref auditTrail).Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					GetAuditTrail(ref auditTrail).Add(AuditEventType.DeadNode, node);
					lastRetryableStatusCode = statusCode;
					lastRetryableNode = node;
					lastException = null;
					continue;
				}

				nodePool.MarkAlive(node);

				var method = endpoint.Method(request);

				if (IsServerError(statusCode, method))
				{
					using var errorStream = await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
					ThrowServerError(statusCode, errorStream, node);
				}

				var bodyStream = await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
				var contentType = responseMessage.Content.Headers.ContentType?.MediaType;

				return endpoint.DeserializeResponse(statusCode, contentType, bodyStream, _serializer);
			}
			catch (Exception ex) when (IsRetryableException(ex, isSync: false, ct))
			{
				sw.Stop();
				HandleRetryableException(ref auditTrail, nodePool, node, ex, sw.Elapsed);
				lastException = ex;
				lastRetryableStatusCode = 0;
				lastRetryableNode = null;

				if (attempt >= maxRetries)
					break;
			}
		}

		return ThrowRetriesExhausted<TResponse>(ref auditTrail, lastException, lastRetryableStatusCode, lastRetryableNode);
	}

	/// <summary>
	/// Returns true for 4xx/5xx responses that should throw, excluding HEAD requests
	/// (status conveyed via deserialization) and GET 404 (valid "not found" with Found=false).
	/// </summary>
	private static bool IsServerError(int statusCode, HttpMethod method) =>
		statusCode >= 400
		&& method != HttpMethod.Head
		&& (method != HttpMethod.Get || statusCode != 404);

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	private static void ThrowServerError(int statusCode, Stream body, Node node)
	{
		string? errorType = null;
		string? reason = null;

		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (root.TryGetProperty("error", out var errorProp))
			{
				if (errorProp.ValueKind == JsonValueKind.Object)
				{
					if (errorProp.TryGetProperty("type", out var typeProp))
						errorType = typeProp.GetString();
					if (errorProp.TryGetProperty("reason", out var reasonProp))
						reason = reasonProp.GetString();
				}
				else if (errorProp.ValueKind == JsonValueKind.String)
				{
					reason = errorProp.GetString();
				}
			}
		}
		catch
		{
			// Body may not be valid JSON; throw with whatever we have.
		}

		throw new OpenSearchServerException(errorType, reason, statusCode, null, node);
	}

	private static bool IsRetryableException(Exception ex, bool isSync, CancellationToken ct = default) =>
		ex is HttpRequestException
		|| (isSync && ex is TaskCanceledException { InnerException: TimeoutException })
		|| (!isSync && ex is TaskCanceledException && !ct.IsCancellationRequested);

	private static void HandleRetryableException(
		ref RequestAuditTrail? auditTrail,
		NodePool nodePool,
		Node node,
		Exception ex,
		TimeSpan duration)
	{
		GetAuditTrail(ref auditTrail).Add(AuditEventType.BadResponse, node, exception: ex, duration: duration);
		nodePool.MarkDead(node);
		GetAuditTrail(ref auditTrail).Add(AuditEventType.DeadNode, node);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	private static TResponse ThrowRetriesExhausted<TResponse>(
		ref RequestAuditTrail? auditTrail,
		Exception? lastException,
		int lastRetryableStatusCode,
		Node? lastRetryableNode)
	{
		GetAuditTrail(ref auditTrail).Add(AuditEventType.AllNodesDead);

		Exception inner;
		if (lastException is not null)
			inner = lastException;
		else if (lastRetryableStatusCode > 0)
			inner = new TransportException(
				$"Received retryable status code {lastRetryableStatusCode} from {lastRetryableNode?.Host}",
				lastRetryableStatusCode, null, lastRetryableNode);
		else
			inner = new InvalidOperationException("No attempts were made.");

		throw new TransportException(
			"Maximum number of retries exhausted. No healthy nodes available.", inner)
		{
			AuditTrail = auditTrail
		};
	}

	private static RequestAuditTrail GetAuditTrail(ref RequestAuditTrail? auditTrail) =>
		auditTrail ??= new RequestAuditTrail();

	private HttpRequestMessage BuildRequestMessage<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		Node node,
		TransportOptions? options)
	{
		var message = CreateBaseRequestMessage(request, endpoint, node, options);

		var body = endpoint.GetBody(request);
		if (body is not null)
		{
			var stream = new MemoryStream();
			body.WriteTo(stream, _serializer);
			stream.Position = 0;
			SetBodyContent(message, body.ContentType, stream);
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
		var message = CreateBaseRequestMessage(request, endpoint, node, options);

		var body = endpoint.GetBody(request);
		if (body is not null)
		{
			var stream = new MemoryStream();
			await body.WriteToAsync(stream, _serializer, ct).ConfigureAwait(false);
			stream.Position = 0;
			SetBodyContent(message, body.ContentType, stream);
		}

		_configuration.OnRequestCreated?.Invoke(message);
		return message;
	}

	private HttpRequestMessage CreateBaseRequestMessage<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		Node node,
		TransportOptions? options)
	{
		var method = MapHttpMethod(endpoint.Method(request));
		var uri = BuildRequestUri(node, endpoint.RequestUrl(request), options);

		var message = new HttpRequestMessage(method, uri);
		ApplyHeaders(message, options);

		if (_cachedAuthHeader is not null)
			message.Headers.Authorization = _cachedAuthHeader;

		if (_configuration.EnableHttpCompression)
			message.Headers.AcceptEncoding.Add(GzipEncoding);

		return message;
	}

	private void SetBodyContent(HttpRequestMessage message, string contentType, MemoryStream stream)
	{
		var content = new StreamContent(stream);
		content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
		message.Content = content;
	}

	private static Uri BuildRequestUri(Node node, string requestUrl, TransportOptions? options)
	{
		var baseUri = node.Host;
		var path = requestUrl.StartsWith('/') ? requestUrl : "/" + requestUrl;

		if (options?.QueryParameters is { Count: > 0 } queryParams)
		{
			var sb = new StringBuilder(path, path.Length + queryParams.Count * 32);
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

		if (options?.Headers is { Count: > 0 } headers)
		{
			foreach (var (key, value) in headers)
				message.Headers.TryAddWithoutValidation(key, value);
		}
	}

	private static AuthenticationHeaderValue? BuildAuthHeader(ITransportConfiguration config)
	{
		if (config.BasicAuth is { } basic)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basic.Username}:{basic.Password}"));
			return new AuthenticationHeaderValue("Basic", encoded);
		}

		if (config.ApiKeyAuth is { } apiKey)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey.Id}:{apiKey.ApiKey}"));
			return new AuthenticationHeaderValue("ApiKey", encoded);
		}

		return null;
	}

	private TransportOptions? MergeOptions(TransportOptions? perRequest)
	{
		if (perRequest is null)
			return DefaultOptions;

		if (DefaultOptions is null)
			return perRequest;

		return new TransportOptions
		{
			Headers = MergeDictionaries(DefaultOptions.Headers, perRequest.Headers),
			QueryParameters = MergeDictionaries(DefaultOptions.QueryParameters, perRequest.QueryParameters),
			WarningsHandler = perRequest.WarningsHandler ?? DefaultOptions.WarningsHandler
		};
	}

	private static Dictionary<string, string>? MergeDictionaries(
		Dictionary<string, string>? defaults,
		Dictionary<string, string>? overrides)
	{
		if (defaults is null && overrides is null)
			return null;

		if (defaults is null)
			return overrides;

		if (overrides is null)
			return defaults;

		var merged = new Dictionary<string, string>(defaults.Count + overrides.Count, StringComparer.OrdinalIgnoreCase);
		foreach (var (key, value) in defaults)
			merged[key] = value;
		foreach (var (key, value) in overrides)
			merged[key] = value;
		return merged;
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

		HttpMessageHandler result = handler;
		if (configuration.HttpMessageHandlerFactory is { } factory)
			result = factory(result);

		return result;
	}

	/// <inheritdoc />
	public void Dispose() => _httpClientFactory.Dispose();
}
