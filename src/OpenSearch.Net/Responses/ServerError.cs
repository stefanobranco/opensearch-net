namespace OpenSearch.Net;

/// <summary>
/// Represents the top-level error envelope returned by OpenSearch.
/// Contains the HTTP status code and structured error details.
/// </summary>
public sealed class ServerError
{
	/// <summary>The structured error detail, if the server returned an object.</summary>
	public ErrorCause? Error { get; set; }

	/// <summary>The HTTP status code from the error response.</summary>
	public int Status { get; set; }

	/// <inheritdoc />
	public override string ToString() =>
		$"ServerError: {Status}" + (Error is not null ? $" {Error}" : "");
}
