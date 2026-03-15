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
		var type = serverError.Error?.Type ?? "unknown_error";
		var msg = serverError.Error?.Reason ?? "No reason provided.";
		return $"OpenSearch returned [{statusCode}] {type}: {msg}";
	}
}
