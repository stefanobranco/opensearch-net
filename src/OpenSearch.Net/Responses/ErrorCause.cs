namespace OpenSearch.Net;

/// <summary>
/// Represents a structured error detail returned by OpenSearch.
/// Contains the error type, human-readable reason, and optional causal chain.
/// </summary>
public sealed class ErrorCause
{
	/// <summary>The error type (e.g., <c>"index_not_found_exception"</c>).</summary>
	public string? Type { get; set; }

	/// <summary>A human-readable explanation of the error.</summary>
	public string? Reason { get; set; }

	/// <summary>The underlying cause of the error, if available.</summary>
	public ErrorCause? CausedBy { get; set; }

	/// <summary>The root causes of the error.</summary>
	public List<ErrorCause>? RootCause { get; set; }

	/// <summary>The server stack trace, present when <c>error_trace=true</c>.</summary>
	public string? StackTrace { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		var msg = $"Type: {Type ?? "(none)"}, Reason: \"{Reason ?? "(none)"}\"";

		if (CausedBy is not null)
			msg += $" CausedBy: [{CausedBy}]";

		return msg;
	}
}
