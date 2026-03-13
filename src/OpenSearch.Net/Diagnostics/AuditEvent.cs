namespace OpenSearch.Net;

/// <summary>
/// The type of event recorded in a request audit trail.
/// </summary>
public enum AuditEventType
{
	/// <summary>A node was selected for the request.</summary>
	NodeSelected,

	/// <summary>The HTTP request was sent.</summary>
	RequestSent,

	/// <summary>An HTTP response was received.</summary>
	ResponseReceived,

	/// <summary>The server returned a non-success status code.</summary>
	BadResponse,

	/// <summary>The request is being retried on another node.</summary>
	Retry,

	/// <summary>A node was marked as dead.</summary>
	DeadNode,

	/// <summary>A previously dead node was revived.</summary>
	ReviveNode,

	/// <summary>All nodes in the pool are dead.</summary>
	AllNodesDead
}

/// <summary>
/// A single event in the diagnostic audit trail of a request.
/// </summary>
public sealed record AuditEvent
{
	/// <summary>The type of audit event.</summary>
	public required AuditEventType Type { get; init; }

	/// <summary>When the event occurred.</summary>
	public required DateTimeOffset Timestamp { get; init; }

	/// <summary>The node involved in this event, if applicable.</summary>
	public Node? Node { get; init; }

	/// <summary>The HTTP status code, if applicable.</summary>
	public int? StatusCode { get; init; }

	/// <summary>The exception that triggered this event, if any.</summary>
	public Exception? Exception { get; init; }

	/// <summary>The duration of the operation, if applicable.</summary>
	public TimeSpan? Duration { get; init; }
}
