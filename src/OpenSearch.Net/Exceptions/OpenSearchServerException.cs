namespace OpenSearch.Net;

/// <summary>
/// Exception thrown when OpenSearch returns an error response with structured error information.
/// Only thrown when <see cref="ITransportConfiguration.ThrowExceptions"/> is <c>true</c>.
/// </summary>
public sealed class OpenSearchServerException : TransportException
{
	/// <summary>
	/// The structured server error from the response.
	/// </summary>
	public ServerError ServerError { get; }

	/// <summary>
	/// The error type returned by OpenSearch (e.g., "index_not_found_exception").
	/// </summary>
	public string? ErrorType => ServerError.Error?.Type;

	/// <summary>
	/// The human-readable reason string returned by OpenSearch.
	/// </summary>
	public string? Reason => ServerError.Error?.Reason;

	/// <summary>
	/// Creates a new <see cref="OpenSearchServerException"/> from a parsed server error.
	/// </summary>
	public OpenSearchServerException(
		ServerError serverError,
		int statusCode,
		Node? node)
		: base(FormatMessage(serverError, statusCode), statusCode, node)
	{
		ServerError = serverError;
	}

	private static string FormatMessage(ServerError serverError, int statusCode)
	{
		var error = serverError.Error;
		var type = error?.Type ?? "unknown_error";
		var msg = error?.Reason ?? "No reason provided.";
		var result = $"OpenSearch returned [{statusCode}] {type}: {msg}";

		// Surface the real error from the causal chain — OpenSearch wraps the actual
		// problem (e.g., mapping errors, parse failures) behind generic messages like
		// "all shards failed".
		var causedBy = error?.CausedBy;
		if (causedBy is not null)
			result += $" CausedBy: {causedBy.Type}: {causedBy.Reason}";
		else if (error?.RootCause is [var first, ..])
			result += $" RootCause: {first.Type}: {first.Reason}";

		return result;
	}
}
