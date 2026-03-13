namespace OpenSearch.Net;

/// <summary>
/// Represents the raw HTTP response from an OpenSearch node.
/// </summary>
public sealed class OpenSearchHttpResponse : IDisposable
{
	/// <summary>
	/// The HTTP status code returned by the server.
	/// </summary>
	public required int StatusCode { get; init; }

	/// <summary>
	/// The response body stream. May be null for responses with no body (e.g., HEAD requests).
	/// </summary>
	public required Stream? Body { get; init; }

	/// <summary>
	/// The response headers.
	/// </summary>
	public required Dictionary<string, IEnumerable<string>> Headers { get; init; }

	/// <summary>
	/// The Content-Type header value, if present.
	/// </summary>
	public string? ContentType { get; init; }

	/// <summary>
	/// Returns true if the status code is in the 2xx range.
	/// </summary>
	public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode < 300;

	/// <inheritdoc />
	public void Dispose() => Body?.Dispose();
}
