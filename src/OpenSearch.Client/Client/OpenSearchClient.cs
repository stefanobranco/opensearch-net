using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// The high-level OpenSearch .NET client. Provides typed request/response methods
/// for all OpenSearch API endpoints.
/// </summary>
/// <remarks>
/// Create an instance using one of the constructors. The simplest form accepts one or more URIs:
/// <code>
/// var client = new OpenSearchClient(new Uri("https://localhost:9200"));
/// </code>
/// For full control, use <see cref="OpenSearchClientSettings.Builder"/>:
/// <code>
/// var settings = OpenSearchClientSettings
///     .Create(new Uri("https://localhost:9200"))
///     .RequestTimeout(TimeSpan.FromSeconds(30))
///     .MaxRetries(3)
///     .Build();
/// var client = new OpenSearchClient(settings);
/// </code>
/// </remarks>
public sealed partial class OpenSearchClient : IDisposable
{
	private readonly IOpenSearchTransport _transport;
	private readonly bool _ownsTransport;
	private readonly IOpenSearchClientSettings _settings;

	/// <summary>
	/// Creates a new client with the given settings and transport.
	/// The caller is responsible for disposing the transport.
	/// </summary>
	public OpenSearchClient(IOpenSearchClientSettings settings, IOpenSearchTransport transport)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(transport);

		_settings = settings;
		_transport = transport;
		_ownsTransport = false;
	}

	/// <summary>
	/// Creates a new client with the given settings, using a default <see cref="HttpClientTransport"/>.
	/// The client owns and will dispose the transport.
	/// </summary>
	public OpenSearchClient(IOpenSearchClientSettings settings)
		: this(settings, new HttpClientTransport(settings))
	{
		_ownsTransport = true;
	}

	/// <summary>
	/// Creates a new client connecting to the specified URIs with default settings.
	/// The client owns and will dispose the transport.
	/// </summary>
	public OpenSearchClient(params Uri[] uris)
		: this(OpenSearchClientSettings.Create(uris).Build())
	{
	}

	/// <summary>
	/// The underlying transport used to send requests.
	/// </summary>
	public IOpenSearchTransport Transport => _transport;

	/// <summary>
	/// The client settings, including serialization configuration.
	/// </summary>
	public IOpenSearchClientSettings Settings => _settings;

	/// <summary>
	/// Executes a synchronous request against the configured OpenSearch cluster.
	/// </summary>
	public TResponse DoRequest<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null) =>
		_transport.PerformRequest(request, endpoint, options);

	/// <summary>
	/// Executes an asynchronous request against the configured OpenSearch cluster.
	/// </summary>
	public Task<TResponse> DoRequestAsync<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null,
		CancellationToken ct = default) =>
		_transport.PerformRequestAsync(request, endpoint, options, ct);

	/// <inheritdoc />
	public void Dispose()
	{
		if (_ownsTransport && _transport is IDisposable disposable)
			disposable.Dispose();
	}
}
