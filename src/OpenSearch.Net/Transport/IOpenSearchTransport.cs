namespace OpenSearch.Net;

/// <summary>
/// Core transport abstraction for sending typed requests to an OpenSearch cluster
/// and receiving typed responses. Handles node selection, retries, serialization,
/// and diagnostics.
/// </summary>
public interface IOpenSearchTransport
{
	/// <summary>
	/// Synchronously sends a request to the cluster and returns the deserialized response.
	/// </summary>
	TResponse PerformRequest<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null);

	/// <summary>
	/// Asynchronously sends a request to the cluster and returns the deserialized response.
	/// </summary>
	Task<TResponse> PerformRequestAsync<TRequest, TResponse>(
		TRequest request,
		IEndpoint<TRequest, TResponse> endpoint,
		TransportOptions? options = null,
		CancellationToken ct = default);

}
