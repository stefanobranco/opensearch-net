namespace OpenSearch.Net;

/// <summary>
/// Configuration settings for the OpenSearch transport layer.
/// </summary>
public interface ITransportConfiguration
{
	/// <summary>
	/// The pool of nodes that the transport will send requests to.
	/// </summary>
	NodePool NodePool { get; }

	/// <summary>
	/// The serializer used to serialize request bodies and deserialize response bodies.
	/// When null, the transport uses a default JSON serializer.
	/// </summary>
	IOpenSearchSerializer? Serializer { get; }

	/// <summary>
	/// The timeout for individual HTTP requests. Default is 60 seconds.
	/// </summary>
	TimeSpan RequestTimeout { get; }

	/// <summary>
	/// How often the underlying HTTP handler is rotated to pick up DNS changes. Default is 5 minutes.
	/// </summary>
	TimeSpan DnsRefreshTimeout { get; }

	/// <summary>
	/// The maximum number of retries before giving up. Default is (number of nodes - 1).
	/// </summary>
	int MaxRetries { get; }

	/// <summary>
	/// Whether to enable gzip compression for request and response bodies.
	/// </summary>
	bool EnableHttpCompression { get; }

	/// <summary>
	/// Basic authentication credentials, if configured.
	/// </summary>
	BasicAuthCredentials? BasicAuth { get; }

	/// <summary>
	/// API key authentication credentials, if configured.
	/// </summary>
	ApiKeyCredentials? ApiKeyAuth { get; }

	/// <summary>
	/// Whether to disable automatic proxy detection. Default is false.
	/// </summary>
	bool DisableAutomaticProxyDetection { get; }

	/// <summary>
	/// The proxy address to use for HTTP requests.
	/// </summary>
	Uri? ProxyAddress { get; }

	/// <summary>
	/// The username for proxy authentication.
	/// </summary>
	string? ProxyUsername { get; }

	/// <summary>
	/// The password for proxy authentication.
	/// </summary>
	string? ProxyPassword { get; }

	/// <summary>
	/// Optional callback invoked on each <see cref="HttpRequestMessage"/> before it is sent.
	/// Useful for custom header injection or logging.
	/// </summary>
	Action<HttpRequestMessage>? OnRequestCreated { get; }

	/// <summary>
	/// Optional factory that wraps the default <see cref="HttpMessageHandler"/>.
	/// Use this to inject delegating handlers (e.g., AWS SigV4 signing) into the HTTP pipeline.
	/// The factory receives the default handler and should return a handler that delegates to it.
	/// </summary>
	Func<HttpMessageHandler, HttpMessageHandler>? HttpMessageHandlerFactory { get; }
}
