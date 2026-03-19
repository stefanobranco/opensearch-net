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

	/// <summary>
	/// The serializer used by this transport for request and response bodies.
	/// </summary>
	public IOpenSearchSerializer Serializer => _serializer;

	/// <summary>
	/// Default transport-level options applied to every request unless overridden.
	/// </summary>
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
		var captureBody = ResolveDisableDirectStreaming(mergedOptions);
		RequestAuditTrail? auditTrail = null;

		Exception? lastException = null;
		int lastRetryableStatusCode = 0;
		Node? lastRetryableNode = null;
		var lastMethod = endpoint.Method(request);
		Uri? lastUri = null;

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

				byte[]? requestBodyBytes = null;
				using var requestMessage = BuildRequestMessage(request, endpoint, node, mergedOptions,
					captureBody, out requestBodyBytes);
				lastUri = requestMessage.RequestUri;
				GetAuditTrail(ref auditTrail).Add(AuditEventType.RequestSent, node);

				using var responseMessage = client.Send(requestMessage, HttpCompletionOption.ResponseHeadersRead);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				var method = endpoint.Method(request);
				GetAuditTrail(ref auditTrail).Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode))
				{
					GetAuditTrail(ref auditTrail).Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					GetAuditTrail(ref auditTrail).Add(AuditEventType.DeadNode, node);

					if (attempt < maxRetries)
					{
						lastRetryableStatusCode = statusCode;
						lastRetryableNode = node;
						lastException = null;
						continue;
					}

					// Last attempt: fall through to server error handling below
					// (node stays dead, but we return the response instead of always throwing)
				}
				else
				{
					nodePool.MarkAlive(node);
				}

				if (IsServerError(statusCode, method))
				{
					var errorBytes = ReadAllBytes(responseMessage.Content.ReadAsStream());
					var serverError = ParseServerError(statusCode, errorBytes);

					if (_configuration.ThrowExceptions)
						ThrowServerError(serverError, statusCode, node);

					var response = (TResponse?)Activator.CreateInstance(typeof(TResponse))
						?? throw new InvalidOperationException($"Cannot create instance of {typeof(TResponse)}");

					AttachApiCallDetails(response, method, requestMessage.RequestUri!, node, statusCode,
						sw.Elapsed, auditTrail, requestBodyBytes, errorBytes, exception: null, serverError);

					return response;
				}

				byte[]? responseBodyBytes = null;
				Stream deserializationStream;
				if (captureBody)
				{
					responseBodyBytes = ReadAllBytes(responseMessage.Content.ReadAsStream());
					deserializationStream = new MemoryStream(responseBodyBytes);
				}
				else
				{
					deserializationStream = responseMessage.Content.ReadAsStream();
				}

				using (deserializationStream)
				{
					var contentType = responseMessage.Content.Headers.ContentType?.MediaType;
					var response = endpoint.DeserializeResponse(statusCode, contentType, deserializationStream, _serializer);

					AttachApiCallDetails(response, method, requestMessage.RequestUri!, node, statusCode,
						sw.Elapsed, auditTrail, requestBodyBytes, responseBodyBytes, exception: null, serverError: null);

					return response;
				}
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

		return ThrowRetriesExhausted<TResponse>(ref auditTrail, lastException, lastRetryableStatusCode, lastRetryableNode, lastMethod, lastUri);
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
		var captureBody = ResolveDisableDirectStreaming(mergedOptions);
		RequestAuditTrail? auditTrail = null;

		Exception? lastException = null;
		int lastRetryableStatusCode = 0;
		Node? lastRetryableNode = null;
		var lastMethod = endpoint.Method(request);
		Uri? lastUri = null;

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

				byte[]? requestBodyBytes = null;
				using var requestMessage = BuildRequestMessage(request, endpoint, node, mergedOptions,
					captureBody, out requestBodyBytes);
				lastUri = requestMessage.RequestUri;
				GetAuditTrail(ref auditTrail).Add(AuditEventType.RequestSent, node);

				using var responseMessage = await client
					.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct)
					.ConfigureAwait(false);
				sw.Stop();

				var statusCode = (int)responseMessage.StatusCode;
				var method = endpoint.Method(request);
				GetAuditTrail(ref auditTrail).Add(AuditEventType.ResponseReceived, node, statusCode, duration: sw.Elapsed);

				ProcessWarningHeaders(responseMessage, mergedOptions);

				if (RetryableStatusCodes.Contains(statusCode))
				{
					GetAuditTrail(ref auditTrail).Add(AuditEventType.BadResponse, node, statusCode);
					nodePool.MarkDead(node);
					GetAuditTrail(ref auditTrail).Add(AuditEventType.DeadNode, node);

					if (attempt < maxRetries)
					{
						lastRetryableStatusCode = statusCode;
						lastRetryableNode = node;
						lastException = null;
						continue;
					}

					// Last attempt: fall through to server error handling below
				}
				else
				{
					nodePool.MarkAlive(node);
				}

				if (IsServerError(statusCode, method))
				{
					var errorBytes = await ReadAllBytesAsync(
						await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false), ct)
						.ConfigureAwait(false);
					var serverError = ParseServerError(statusCode, errorBytes);

					if (_configuration.ThrowExceptions)
						ThrowServerError(serverError, statusCode, node);

					var response = (TResponse?)Activator.CreateInstance(typeof(TResponse))
						?? throw new InvalidOperationException($"Cannot create instance of {typeof(TResponse)}");

					AttachApiCallDetails(response, method, requestMessage.RequestUri!, node, statusCode,
						sw.Elapsed, auditTrail, requestBodyBytes, errorBytes, exception: null, serverError);

					return response;
				}

				byte[]? responseBodyBytes = null;
				Stream deserializationStream;
				if (captureBody)
				{
					responseBodyBytes = await ReadAllBytesAsync(
						await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false), ct)
						.ConfigureAwait(false);
					deserializationStream = new MemoryStream(responseBodyBytes);
				}
				else
				{
					deserializationStream = await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
				}

				using (deserializationStream)
				{
					var contentType = responseMessage.Content.Headers.ContentType?.MediaType;
					var response = endpoint.DeserializeResponse(statusCode, contentType, deserializationStream, _serializer);

					AttachApiCallDetails(response, method, requestMessage.RequestUri!, node, statusCode,
						sw.Elapsed, auditTrail, requestBodyBytes, responseBodyBytes, exception: null, serverError: null);

					return response;
				}
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

		return ThrowRetriesExhausted<TResponse>(ref auditTrail, lastException, lastRetryableStatusCode, lastRetryableNode, lastMethod, lastUri);
	}

	/// <summary>
	/// Returns true for 4xx/5xx responses that should be treated as server errors, excluding HEAD requests
	/// (status conveyed via deserialization) and GET/DELETE 404 (valid "not found" with Found=false).
	/// </summary>
	private static bool IsServerError(int statusCode, HttpMethod method) =>
		statusCode >= 400
		&& method != HttpMethod.Head
		&& (statusCode != 404 || (method != HttpMethod.Get && method != HttpMethod.Delete));

	/// <summary>
	/// Parses a server error response body into a <see cref="ServerError"/> object.
	/// </summary>
	private static ServerError ParseServerError(int statusCode, byte[] body)
	{
		ErrorCause? errorCause = null;

		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (root.TryGetProperty("error", out var errorProp))
			{
				if (errorProp.ValueKind == JsonValueKind.Object)
				{
					errorCause = ReadErrorCause(errorProp);
				}
				else if (errorProp.ValueKind == JsonValueKind.String)
				{
					errorCause = new ErrorCause { Reason = errorProp.GetString() };
				}
			}
		}
		catch
		{
			// Body may not be valid JSON; return with whatever we have.
		}

		return new ServerError { Error = errorCause, Status = statusCode };
	}

	private static ErrorCause ReadErrorCause(JsonElement element)
	{
		var cause = new ErrorCause();

		if (element.TryGetProperty("type", out var typeProp))
			cause.Type = typeProp.GetString();

		if (element.TryGetProperty("reason", out var reasonProp))
			cause.Reason = reasonProp.GetString();

		if (element.TryGetProperty("stack_trace", out var stackProp))
			cause.StackTrace = stackProp.GetString();

		if (element.TryGetProperty("caused_by", out var causedByProp) && causedByProp.ValueKind == JsonValueKind.Object)
			cause.CausedBy = ReadErrorCause(causedByProp);

		if (element.TryGetProperty("root_cause", out var rootCauseProp) && rootCauseProp.ValueKind == JsonValueKind.Array)
		{
			cause.RootCause = [];
			foreach (var item in rootCauseProp.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.Object)
					cause.RootCause.Add(ReadErrorCause(item));
			}
		}

		return cause;
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	private static void ThrowServerError(ServerError serverError, int statusCode, Node node)
	{
		throw new OpenSearchServerException(serverError, statusCode, node);
	}

	private static bool IsRetryableException(Exception ex, bool isSync, CancellationToken ct = default) =>
		ex is HttpRequestException
		|| (isSync && ex is TaskCanceledException { InnerException: TimeoutException })
		|| (!isSync && ex is TaskCanceledException && !ct.IsCancellationRequested);

	private static void HandleRetryableException(
		ref RequestAuditTrail? auditTrail,
		INodePool nodePool,
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
		Node? lastRetryableNode,
		HttpMethod lastMethod,
		Uri? lastUri)
	{
		GetAuditTrail(ref auditTrail).Add(AuditEventType.AllNodesDead);

		Exception inner;
		if (lastException is not null)
			inner = lastException;
		else if (lastRetryableStatusCode > 0)
			inner = new TransportException(
				$"Received retryable status code {lastRetryableStatusCode} from {lastRetryableNode?.Host}",
				lastRetryableStatusCode, lastRetryableNode);
		else
			inner = new InvalidOperationException("No attempts were made.");

		var callDetails = new ApiCallDetails
		{
			HttpMethod = lastMethod,
			Uri = lastUri,
			HttpStatusCode = lastRetryableStatusCode,
			Node = lastRetryableNode,
			Success = false,
			AuditTrail = auditTrail,
			OriginalException = lastException
		};

		throw new TransportException(
			"Maximum number of retries exhausted. No healthy nodes available.", inner)
		{
			AuditTrail = auditTrail,
			ApiCallDetails = callDetails
		};
	}

	private static RequestAuditTrail GetAuditTrail(ref RequestAuditTrail? auditTrail) =>
		auditTrail ??= new RequestAuditTrail();

	private HttpRequestMessage BuildRequestMessage<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		Node node,
		TransportOptions? options,
		bool captureBody,
		out byte[]? requestBodyBytes)
	{
		var message = CreateBaseRequestMessage(request, endpoint, node, options);
		requestBodyBytes = null;

		var body = endpoint.GetBody(request);
		if (body is not null)
		{
			if (captureBody)
			{
				using var ms = new MemoryStream();
				body.WriteTo(ms, _serializer);
				requestBodyBytes = ms.ToArray();
				message.Content = new ByteArrayContent(requestBodyBytes);
				message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(body.ContentType);
			}
			else
			{
				message.Content = new RequestBodyContent(body, _serializer);
			}
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

	private bool ResolveDisableDirectStreaming(TransportOptions? mergedOptions) =>
		mergedOptions?.DisableDirectStreaming ?? _configuration.DisableDirectStreaming;

	private static void AttachApiCallDetails<TResponse>(
		TResponse response,
		HttpMethod method,
		Uri uri,
		Node node,
		int statusCode,
		TimeSpan duration,
		RequestAuditTrail? auditTrail,
		byte[]? requestBodyBytes,
		byte[]? responseBodyBytes,
		Exception? exception,
		ServerError? serverError)
	{
		if (response is null) return;

		var details = new ApiCallDetails
		{
			HttpMethod = method,
			Uri = uri,
			Node = node,
			HttpStatusCode = statusCode,
			// On the error path (serverError != null), Success is always false.
			// On the non-error path, 404 is valid for GET/DELETE/HEAD ("not found" is a valid response).
			Success = serverError is null && statusCode is (>= 200 and < 300) or 404,
			Duration = duration,
			AuditTrail = auditTrail,
			RequestBodyBytes = requestBodyBytes,
			ResponseBodyBytes = responseBodyBytes,
			OriginalException = exception
		};

		// Populate base class properties directly (all response types inherit OpenSearchResponse)
		if (response is OpenSearchResponse osResponse)
		{
			osResponse.ApiCall = details;
			osResponse.ServerError = serverError;
		}
	}

	private static byte[] ReadAllBytes(Stream stream)
	{
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return ms.ToArray();
	}

	private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
	{
		using var ms = new MemoryStream();
		await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
		return ms.ToArray();
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
			WarningsHandler = perRequest.WarningsHandler ?? DefaultOptions.WarningsHandler,
			DisableDirectStreaming = perRequest.DisableDirectStreaming ?? DefaultOptions.DisableDirectStreaming
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

		if (configuration.SkipCertificateValidation)
		{
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
		}
		else if (configuration.ServerCertificateValidationCallback is { } callback)
		{
			handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
			{
				// The sender for SocketsHttpHandler is the SslStream, not the HttpRequestMessage.
				// Pass null for the request message — callers needing request context should use
				// HttpMessageHandlerFactory with a DelegatingHandler instead.
				var cert2 = cert as System.Security.Cryptography.X509Certificates.X509Certificate2;
				return callback(null!, cert2, chain, errors);
			};
		}

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
