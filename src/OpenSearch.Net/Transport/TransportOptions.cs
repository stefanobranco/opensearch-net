namespace OpenSearch.Net;

/// <summary>
/// Per-request options that can override transport defaults.
/// </summary>
public sealed record TransportOptions
{
	/// <summary>
	/// Additional HTTP headers to include in the request.
	/// </summary>
	public Dictionary<string, string>? Headers { get; init; }

	/// <summary>
	/// Additional query string parameters to append to the request URL.
	/// </summary>
	public Dictionary<string, string>? QueryParameters { get; init; }

	/// <summary>
	/// Callback invoked with any warning headers returned by the server.
	/// </summary>
	public Action<string>? WarningsHandler { get; init; }
}
