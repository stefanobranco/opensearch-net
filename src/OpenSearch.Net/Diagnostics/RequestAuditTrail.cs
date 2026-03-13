namespace OpenSearch.Net;

/// <summary>
/// Records a sequence of diagnostic audit events for a single transport-level request,
/// including node selection, retries, and failures.
/// </summary>
public sealed class RequestAuditTrail
{
	private readonly List<AuditEvent> _events = [];

	/// <summary>
	/// The ordered list of events recorded for this request.
	/// </summary>
	public IReadOnlyList<AuditEvent> Events => _events;

	/// <summary>
	/// Adds a pre-constructed audit event.
	/// </summary>
	public void Add(AuditEvent evt) => _events.Add(evt);

	/// <summary>
	/// Adds an audit event from its constituent parts.
	/// </summary>
	public void Add(
		AuditEventType type,
		Node? node = null,
		int? statusCode = null,
		Exception? exception = null,
		TimeSpan? duration = null) =>
		_events.Add(new AuditEvent
		{
			Type = type,
			Timestamp = DateTimeOffset.UtcNow,
			Node = node,
			StatusCode = statusCode,
			Exception = exception,
			Duration = duration
		});
}
