namespace OpenSearch.Net;

/// <summary>
/// Captures transport-level details about a single API call: HTTP method, URI, status code,
/// timing, audit trail, and optionally request/response bodies (when <c>DisableDirectStreaming</c> is enabled).
/// </summary>
public sealed class ApiCallDetails
{
	/// <summary>The HTTP method used for the request.</summary>
	public HttpMethod HttpMethod { get; init; }

	/// <summary>The full URI the request was sent to.</summary>
	public Uri? Uri { get; init; }

	/// <summary>The node that handled the request.</summary>
	public Node? Node { get; init; }

	/// <summary>The HTTP status code returned by the server, or 0 if no response was received.</summary>
	public int HttpStatusCode { get; init; }

	/// <summary>Whether the response indicates a successful call (2xx).</summary>
	public bool Success { get; init; }

	/// <summary>The wall-clock duration of the HTTP call.</summary>
	public TimeSpan Duration { get; init; }

	/// <summary>The audit trail of events for this request, including retries.</summary>
	public RequestAuditTrail? AuditTrail { get; init; }

	/// <summary>
	/// The raw request body bytes. Only populated when <c>DisableDirectStreaming</c> is enabled.
	/// </summary>
	public byte[]? RequestBodyBytes { get; init; }

	/// <summary>
	/// The raw response body bytes. Only populated when <c>DisableDirectStreaming</c> is enabled.
	/// </summary>
	public byte[]? ResponseBodyBytes { get; init; }

	/// <summary>
	/// The parsed server error, when OpenSearch returned a structured error response (4xx/5xx).
	/// </summary>
	public ServerError? ServerError { get; init; }

	/// <summary>
	/// The exception that occurred during the request, if any.
	/// </summary>
	public Exception? OriginalException { get; init; }
}
