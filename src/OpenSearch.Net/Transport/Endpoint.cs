namespace OpenSearch.Net;

/// <summary>
/// Describes how a typed request maps to an HTTP call and how the response is deserialized.
/// Each OpenSearch API endpoint implements this interface (typically via code generation).
/// </summary>
public interface IEndpoint<TRequest, TResponse>
{
	/// <summary>
	/// Returns the HTTP method for the given request.
	/// </summary>
	HttpMethod Method(TRequest request);

	/// <summary>
	/// Returns the relative URL path for the given request (e.g., "/my-index/_search").
	/// </summary>
	string RequestUrl(TRequest request);

	/// <summary>
	/// The content type for the request body, or null if the endpoint has no body.
	/// </summary>
	string? ContentType { get; }

	/// <summary>
	/// Returns the request body, or null if the endpoint sends no body.
	/// </summary>
	RequestBody? GetBody(TRequest request);

	/// <summary>
	/// Deserializes the response body into the strongly-typed response object.
	/// </summary>
	TResponse DeserializeResponse(int statusCode, string? contentType, Stream body, IOpenSearchSerializer serializer);
}

/// <summary>
/// A delegate-based implementation of <see cref="IEndpoint{TRequest, TResponse}"/> for
/// simple or ad-hoc endpoints.
/// </summary>
public sealed class SimpleEndpoint<TRequest, TResponse> : IEndpoint<TRequest, TResponse>
{
	private readonly Func<TRequest, HttpMethod> _method;
	private readonly Func<TRequest, string> _requestUrl;
	private readonly Func<TRequest, RequestBody?>? _getBody;
	private readonly Func<int, string?, Stream, IOpenSearchSerializer, TResponse> _deserialize;
	private readonly string? _contentType;

	/// <summary>
	/// Creates a new <see cref="SimpleEndpoint{TRequest, TResponse}"/> from delegate functions.
	/// </summary>
	public SimpleEndpoint(
		Func<TRequest, HttpMethod> method,
		Func<TRequest, string> requestUrl,
		Func<int, string?, Stream, IOpenSearchSerializer, TResponse> deserialize,
		string? contentType = null,
		Func<TRequest, RequestBody?>? getBody = null)
	{
		ArgumentNullException.ThrowIfNull(method);
		ArgumentNullException.ThrowIfNull(requestUrl);
		ArgumentNullException.ThrowIfNull(deserialize);

		_method = method;
		_requestUrl = requestUrl;
		_deserialize = deserialize;
		_contentType = contentType;
		_getBody = getBody;
	}

	/// <inheritdoc />
	public HttpMethod Method(TRequest request) => _method(request);

	/// <inheritdoc />
	public string RequestUrl(TRequest request) => _requestUrl(request);

	/// <inheritdoc />
	public string? ContentType => _contentType;

	/// <inheritdoc />
	public RequestBody? GetBody(TRequest request) => _getBody?.Invoke(request);

	/// <inheritdoc />
	public TResponse DeserializeResponse(int statusCode, string? contentType, Stream body, IOpenSearchSerializer serializer) =>
		_deserialize(statusCode, contentType, body, serializer);
}
