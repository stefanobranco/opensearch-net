namespace OpenSearch.Net;

/// <summary>
/// Immutable configuration for the OpenSearch transport layer.
/// Use <see cref="Create(NodePool)"/> or <see cref="Create(Uri[])"/> to obtain a <see cref="Builder"/>.
/// </summary>
public sealed class TransportConfiguration : ITransportConfiguration
{
	/// <inheritdoc />
	public INodePool NodePool { get; }

	/// <inheritdoc />
	public IOpenSearchSerializer? Serializer { get; }

	/// <inheritdoc />
	public TimeSpan RequestTimeout { get; }

	/// <inheritdoc />
	public TimeSpan DnsRefreshTimeout { get; }

	/// <inheritdoc />
	public int MaxRetries { get; }

	/// <inheritdoc />
	public bool EnableHttpCompression { get; }

	/// <inheritdoc />
	public BasicAuthCredentials? BasicAuth { get; }

	/// <inheritdoc />
	public ApiKeyCredentials? ApiKeyAuth { get; }

	/// <inheritdoc />
	public bool DisableAutomaticProxyDetection { get; }

	/// <inheritdoc />
	public Uri? ProxyAddress { get; }

	/// <inheritdoc />
	public string? ProxyUsername { get; }

	/// <inheritdoc />
	public string? ProxyPassword { get; }

	/// <inheritdoc />
	public Action<HttpRequestMessage>? OnRequestCreated { get; }

	/// <inheritdoc />
	public Func<HttpMessageHandler, HttpMessageHandler>? HttpMessageHandlerFactory { get; }

	/// <inheritdoc />
	public bool DisableDirectStreaming { get; }

	/// <inheritdoc />
	public bool ThrowExceptions { get; }

	private TransportConfiguration(
		INodePool nodePool,
		IOpenSearchSerializer? serializer,
		TimeSpan requestTimeout,
		TimeSpan dnsRefreshTimeout,
		int maxRetries,
		bool enableHttpCompression,
		BasicAuthCredentials? basicAuth,
		ApiKeyCredentials? apiKeyAuth,
		bool disableAutomaticProxyDetection,
		Uri? proxyAddress,
		string? proxyUsername,
		string? proxyPassword,
		Action<HttpRequestMessage>? onRequestCreated,
		Func<HttpMessageHandler, HttpMessageHandler>? httpMessageHandlerFactory,
		bool disableDirectStreaming,
		bool throwExceptions)
	{
		NodePool = nodePool;
		Serializer = serializer;
		RequestTimeout = requestTimeout;
		DnsRefreshTimeout = dnsRefreshTimeout;
		MaxRetries = maxRetries;
		EnableHttpCompression = enableHttpCompression;
		BasicAuth = basicAuth;
		ApiKeyAuth = apiKeyAuth;
		DisableAutomaticProxyDetection = disableAutomaticProxyDetection;
		ProxyAddress = proxyAddress;
		ProxyUsername = proxyUsername;
		ProxyPassword = proxyPassword;
		OnRequestCreated = onRequestCreated;
		HttpMessageHandlerFactory = httpMessageHandlerFactory;
		DisableDirectStreaming = disableDirectStreaming;
		ThrowExceptions = throwExceptions;
	}

	/// <summary>
	/// Creates a new configuration builder with the given node pool.
	/// </summary>
	public static Builder Create(NodePool nodePool) => new(nodePool);

	/// <summary>
	/// Creates a new configuration builder with a node pool from the given URIs.
	/// </summary>
	public static Builder Create(params Uri[] uris) => new(new NodePool(uris));

	/// <summary>
	/// Fluent builder for constructing <see cref="TransportConfiguration"/> instances.
	/// </summary>
	public sealed class Builder
	{
		private readonly NodePool _nodePool;
		private IOpenSearchSerializer? _serializer;
		private TimeSpan _requestTimeout = TimeSpan.FromSeconds(60);
		private TimeSpan _dnsRefreshTimeout = TimeSpan.FromMinutes(5);
		private int? _maxRetries;
		private bool _enableHttpCompression;
		private BasicAuthCredentials? _basicAuth;
		private ApiKeyCredentials? _apiKeyAuth;
		private bool _disableAutomaticProxyDetection;
		private Uri? _proxyAddress;
		private string? _proxyUsername;
		private string? _proxyPassword;
		private Action<HttpRequestMessage>? _onRequestCreated;
		private Func<HttpMessageHandler, HttpMessageHandler>? _httpMessageHandlerFactory;
		private bool _disableDirectStreaming;
		private bool _throwExceptions;

		internal Builder(NodePool nodePool)
		{
			ArgumentNullException.ThrowIfNull(nodePool);
			_nodePool = nodePool;
		}

		/// <summary>Sets the serializer for request and response bodies.</summary>
		public Builder Serializer(IOpenSearchSerializer serializer)
		{
			_serializer = serializer;
			return this;
		}

		/// <summary>Sets the per-request timeout. Default is 60 seconds.</summary>
		public Builder RequestTimeout(TimeSpan timeout)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero, nameof(timeout));
			_requestTimeout = timeout;
			return this;
		}

		/// <summary>Sets how often the HTTP handler is rotated for DNS refresh. Default is 5 minutes.</summary>
		public Builder DnsRefreshTimeout(TimeSpan timeout)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero, nameof(timeout));
			_dnsRefreshTimeout = timeout;
			return this;
		}

		/// <summary>Sets the maximum number of retries. Default is (node count - 1).</summary>
		public Builder MaxRetries(int retries)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(retries, nameof(retries));
			_maxRetries = retries;
			return this;
		}

		/// <summary>Enables gzip compression for request and response bodies.</summary>
		public Builder EnableHttpCompression(bool enable = true)
		{
			_enableHttpCompression = enable;
			return this;
		}

		/// <summary>Sets HTTP Basic authentication credentials.</summary>
		public Builder Authentication(BasicAuthCredentials credentials)
		{
			_basicAuth = credentials;
			_apiKeyAuth = null;
			return this;
		}

		/// <summary>Sets API key authentication credentials.</summary>
		public Builder Authentication(ApiKeyCredentials credentials)
		{
			_apiKeyAuth = credentials;
			_basicAuth = null;
			return this;
		}

		/// <summary>Disables automatic proxy detection.</summary>
		public Builder DisableAutomaticProxyDetection(bool disable = true)
		{
			_disableAutomaticProxyDetection = disable;
			return this;
		}

		/// <summary>Sets the proxy address and optional credentials.</summary>
		public Builder Proxy(Uri address, string? username = null, string? password = null)
		{
			_proxyAddress = address;
			_proxyUsername = username;
			_proxyPassword = password;
			return this;
		}

		/// <summary>Sets a callback invoked on each request message before it is sent.</summary>
		public Builder OnRequestCreated(Action<HttpRequestMessage> callback)
		{
			_onRequestCreated = callback;
			return this;
		}

		/// <summary>Sets a factory that wraps the default HTTP handler (e.g., for SigV4 signing).</summary>
		public Builder HttpMessageHandlerFactory(Func<HttpMessageHandler, HttpMessageHandler> factory)
		{
			_httpMessageHandlerFactory = factory;
			return this;
		}

		/// <summary>
		/// Enables buffering of request and response bodies so they are captured in
		/// <see cref="ApiCallDetails"/>. Useful for debugging; has a memory cost proportional to body size.
		/// </summary>
		public Builder DisableDirectStreaming(bool disable = true)
		{
			_disableDirectStreaming = disable;
			return this;
		}

		/// <summary>
		/// When enabled, the transport throws <see cref="OpenSearchServerException"/> on HTTP 4xx/5xx
		/// errors instead of returning a response with <see cref="OpenSearchResponse.IsValid"/> = <c>false</c>.
		/// Default is <c>false</c>.
		/// </summary>
		public Builder ThrowExceptions(bool throwExceptions = true)
		{
			_throwExceptions = throwExceptions;
			return this;
		}

		/// <summary>
		/// Builds the immutable <see cref="TransportConfiguration"/>.
		/// </summary>
		public TransportConfiguration Build() =>
			new(
				_nodePool,
				_serializer,
				_requestTimeout,
				_dnsRefreshTimeout,
				_maxRetries ?? Math.Max(0, _nodePool.Nodes.Count - 1),
				_enableHttpCompression,
				_basicAuth,
				_apiKeyAuth,
				_disableAutomaticProxyDetection,
				_proxyAddress,
				_proxyUsername,
				_proxyPassword,
				_onRequestCreated,
				_httpMessageHandlerFactory,
				_disableDirectStreaming,
				_throwExceptions);
	}
}
