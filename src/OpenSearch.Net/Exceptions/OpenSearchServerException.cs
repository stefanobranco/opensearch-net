namespace OpenSearch.Net;

/// <summary>
/// Exception thrown when OpenSearch returns an error response with structured error information.
/// </summary>
public sealed class OpenSearchServerException : TransportException
{
	/// <summary>
	/// The error type returned by OpenSearch (e.g., "index_not_found_exception").
	/// </summary>
	public string? ErrorType { get; }

	/// <summary>
	/// The human-readable reason string returned by OpenSearch.
	/// </summary>
	public string? Reason { get; }

	/// <summary>
	/// Creates a new <see cref="OpenSearchServerException"/> from server error details.
	/// </summary>
	public OpenSearchServerException(
		string? errorType,
		string? reason,
		int statusCode,
		OpenSearchHttpResponse? response,
		Node? node)
		: base(FormatMessage(errorType, reason, statusCode), statusCode, response, node)
	{
		ErrorType = errorType;
		Reason = reason;
	}

	private static string FormatMessage(string? errorType, string? reason, int statusCode)
	{
		var type = errorType ?? "unknown_error";
		var msg = reason ?? "No reason provided.";
		return $"OpenSearch returned [{statusCode}] {type}: {msg}";
	}
}
