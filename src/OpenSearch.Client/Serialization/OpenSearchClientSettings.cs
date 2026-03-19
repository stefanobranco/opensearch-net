using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Immutable configuration for <see cref="OpenSearchClient"/>.
/// Combines transport settings with client-level serialization configuration.
/// Create instances using <see cref="Builder"/> via <see cref="Create(NodePool)"/>
/// or <see cref="Create(Uri[])"/>.
/// </summary>
public sealed class OpenSearchClientSettings : IOpenSearchClientSettings
{
	private readonly ITransportConfiguration _transport;

	private OpenSearchClientSettings(
		ITransportConfiguration transport,
		IOpenSearchSerializer requestResponseSerializer,
		IOpenSearchSerializer sourceSerializer,
		JsonSerializerOptions requestResponseOptions)
	{
		_transport = transport;
		RequestResponseSerializer = requestResponseSerializer;
		SourceSerializer = sourceSerializer;
		RequestResponseOptions = requestResponseOptions;
	}

	// --- IOpenSearchClientSettings ---

	/// <inheritdoc />
	public IOpenSearchSerializer RequestResponseSerializer { get; }

	/// <inheritdoc />
	public IOpenSearchSerializer SourceSerializer { get; }

	/// <inheritdoc />
	public JsonSerializerOptions RequestResponseOptions { get; }

	// --- ITransportConfiguration delegation ---

	/// <inheritdoc />
	public INodePool NodePool => _transport.NodePool;

	/// <inheritdoc />
	public IOpenSearchSerializer? Serializer => RequestResponseSerializer;

	/// <inheritdoc />
	public TimeSpan RequestTimeout => _transport.RequestTimeout;

	/// <inheritdoc />
	public TimeSpan DnsRefreshTimeout => _transport.DnsRefreshTimeout;

	/// <inheritdoc />
	public int MaxRetries => _transport.MaxRetries;

	/// <inheritdoc />
	public bool EnableHttpCompression => _transport.EnableHttpCompression;

	/// <inheritdoc />
	public BasicAuthCredentials? BasicAuth => _transport.BasicAuth;

	/// <inheritdoc />
	public ApiKeyCredentials? ApiKeyAuth => _transport.ApiKeyAuth;

	/// <inheritdoc />
	public bool DisableAutomaticProxyDetection => _transport.DisableAutomaticProxyDetection;

	/// <inheritdoc />
	public Uri? ProxyAddress => _transport.ProxyAddress;

	/// <inheritdoc />
	public string? ProxyUsername => _transport.ProxyUsername;

	/// <inheritdoc />
	public string? ProxyPassword => _transport.ProxyPassword;

	/// <inheritdoc />
	public Action<HttpRequestMessage>? OnRequestCreated => _transport.OnRequestCreated;

	/// <inheritdoc />
	public Func<HttpMessageHandler, HttpMessageHandler>? HttpMessageHandlerFactory => _transport.HttpMessageHandlerFactory;

	/// <inheritdoc />
	public bool DisableDirectStreaming => _transport.DisableDirectStreaming;

	/// <inheritdoc />
	public bool ThrowExceptions => _transport.ThrowExceptions;

	/// <inheritdoc />
	public Func<HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, System.Net.Security.SslPolicyErrors, bool>? ServerCertificateValidationCallback => _transport.ServerCertificateValidationCallback;

	/// <inheritdoc />
	public bool SkipCertificateValidation => _transport.SkipCertificateValidation;

	/// <summary>
	/// Creates a builder configured with the given <paramref name="nodePool"/>.
	/// </summary>
	public static Builder Create(NodePool nodePool) => new(nodePool);

	/// <summary>
	/// Creates a builder configured with a <see cref="NodePool"/> constructed from the given URIs.
	/// </summary>
	public static Builder Create(params Uri[] uris) => new(uris);

	/// <summary>
	/// Fluent builder for <see cref="OpenSearchClientSettings"/>.
	/// </summary>
	public sealed class Builder
	{
		private readonly TransportConfiguration.Builder _transportBuilder;
		private Func<IOpenSearchSerializer>? _sourceSerializerFactory;

		/// <summary>
		/// Creates a builder with the given <paramref name="nodePool"/>.
		/// </summary>
		public Builder(NodePool nodePool)
		{
			_transportBuilder = TransportConfiguration.Create(nodePool);
		}

		/// <summary>
		/// Creates a builder with a <see cref="NodePool"/> from the given URIs.
		/// </summary>
		public Builder(params Uri[] uris) : this(new NodePool(uris))
		{
		}

		/// <summary>
		/// Sets the per-request timeout. Default is 60 seconds.
		/// </summary>
		public Builder RequestTimeout(TimeSpan timeout)
		{
			_transportBuilder.RequestTimeout(timeout);
			return this;
		}

		/// <summary>
		/// Sets how often the HTTP handler is rotated for DNS refresh. Default is 5 minutes.
		/// </summary>
		public Builder DnsRefreshTimeout(TimeSpan timeout)
		{
			_transportBuilder.DnsRefreshTimeout(timeout);
			return this;
		}

		/// <summary>
		/// Sets the maximum number of retries per request.
		/// </summary>
		public Builder MaxRetries(int retries)
		{
			_transportBuilder.MaxRetries(retries);
			return this;
		}

		/// <summary>
		/// Enables HTTP compression (gzip) for request and response bodies.
		/// </summary>
		public Builder EnableHttpCompression(bool enable = true)
		{
			_transportBuilder.EnableHttpCompression(enable);
			return this;
		}

		/// <summary>
		/// Configures HTTP Basic authentication credentials.
		/// </summary>
		public Builder Authentication(BasicAuthCredentials credentials)
		{
			_transportBuilder.Authentication(credentials);
			return this;
		}

		/// <summary>
		/// Configures API key authentication credentials.
		/// </summary>
		public Builder Authentication(ApiKeyCredentials credentials)
		{
			_transportBuilder.Authentication(credentials);
			return this;
		}

		/// <summary>
		/// Disables automatic proxy detection.
		/// </summary>
		public Builder DisableAutomaticProxyDetection(bool disable = true)
		{
			_transportBuilder.DisableAutomaticProxyDetection(disable);
			return this;
		}

		/// <summary>
		/// Sets the proxy address and optional credentials for HTTP requests.
		/// </summary>
		public Builder Proxy(Uri address, string? username = null, string? password = null)
		{
			_transportBuilder.Proxy(address, username, password);
			return this;
		}

		/// <summary>
		/// Sets a callback invoked on each <see cref="HttpRequestMessage"/> before it is sent.
		/// </summary>
		public Builder OnRequestCreated(Action<HttpRequestMessage> callback)
		{
			_transportBuilder.OnRequestCreated(callback);
			return this;
		}

		/// <summary>
		/// Sets a factory that wraps the default HTTP handler (e.g., for AWS SigV4 signing).
		/// </summary>
		public Builder HttpMessageHandlerFactory(Func<HttpMessageHandler, HttpMessageHandler> factory)
		{
			_transportBuilder.HttpMessageHandlerFactory(factory);
			return this;
		}

		/// <summary>
		/// Enables buffering of request and response bodies so they are captured in
		/// <see cref="ApiCallDetails"/>. Useful for debugging; has a memory cost proportional to body size.
		/// </summary>
		public Builder DisableDirectStreaming(bool disable = true)
		{
			_transportBuilder.DisableDirectStreaming(disable);
			return this;
		}

		/// <summary>
		/// When enabled, the transport throws <see cref="OpenSearchServerException"/> on HTTP 4xx/5xx
		/// errors instead of returning a response with <see cref="OpenSearchResponse.IsValid"/> = <c>false</c>.
		/// Default is <c>false</c>.
		/// </summary>
		public Builder ThrowExceptions(bool throwExceptions = true)
		{
			_transportBuilder.ThrowExceptions(throwExceptions);
			return this;
		}

		/// <summary>Skips all SSL certificate validation. Use for self-signed certs or internal CAs.</summary>
		public Builder SkipCertificateValidation(bool skip = true)
		{
			_transportBuilder.SkipCertificateValidation(skip);
			return this;
		}

		/// <summary>
		/// Configures a custom source serializer factory. The source serializer is used for user
		/// document types (e.g., <c>_source</c> fields). If not set, the request/response
		/// serializer is used for all types.
		/// </summary>
		public Builder SourceSerializer(Func<IOpenSearchSerializer> factory)
		{
			ArgumentNullException.ThrowIfNull(factory);
			_sourceSerializerFactory = factory;
			return this;
		}

		/// <summary>
		/// Builds an immutable <see cref="OpenSearchClientSettings"/> instance.
		/// </summary>
		public OpenSearchClientSettings Build()
		{
			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				NumberHandling = JsonNumberHandling.AllowReadingFromString,
			};

			var transport = _transportBuilder.Build();
			var requestResponseSerializer = new SystemTextJsonSerializer(jsonOptions);
			var sourceSerializer = _sourceSerializerFactory?.Invoke() ?? requestResponseSerializer;

			var settings = new OpenSearchClientSettings(
				transport,
				requestResponseSerializer,
				sourceSerializer,
				jsonOptions);

			// Add converters after construction so the context provider can capture the settings instance.
			jsonOptions.Converters.Add(new ContextProvider<IOpenSearchClientSettings>(settings));
			jsonOptions.Converters.Add(new JsonEnumConverterFactory());
			jsonOptions.Converters.Add(new ServerErrorConverter());

			return settings;
		}
	}
}
