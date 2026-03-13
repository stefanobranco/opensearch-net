namespace OpenSearch.Net;

/// <summary>
/// Exception thrown when the transport layer encounters an error communicating
/// with an OpenSearch cluster.
/// </summary>
public class TransportException : Exception
{
	/// <summary>
	/// The HTTP status code returned by the server, if available.
	/// </summary>
	public int? StatusCode { get; }

	/// <summary>
	/// The raw HTTP response associated with this exception, if available.
	/// </summary>
	public OpenSearchHttpResponse? Response { get; }

	/// <summary>
	/// The node that the failed request was sent to, if available.
	/// </summary>
	public Node? Node { get; }

	/// <summary>
	/// The audit trail of events leading up to this exception.
	/// </summary>
	public RequestAuditTrail? AuditTrail { get; init; }

	/// <summary>
	/// Creates a new <see cref="TransportException"/> with the given message.
	/// </summary>
	public TransportException(string message) : base(message) { }

	/// <summary>
	/// Creates a new <see cref="TransportException"/> with the given message and inner exception.
	/// </summary>
	public TransportException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// Creates a new <see cref="TransportException"/> with full context from a failed request.
	/// </summary>
	public TransportException(string message, int statusCode, OpenSearchHttpResponse? response, Node? node)
		: base(message)
	{
		StatusCode = statusCode;
		Response = response;
		Node = node;
	}

	/// <summary>
	/// Creates a new <see cref="TransportException"/> with full context and an inner exception.
	/// </summary>
	public TransportException(string message, int statusCode, OpenSearchHttpResponse? response, Node? node, Exception innerException)
		: base(message, innerException)
	{
		StatusCode = statusCode;
		Response = response;
		Node = node;
	}
}
