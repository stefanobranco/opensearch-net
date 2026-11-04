using System.Text.Json.Serialization;

namespace OpenSearch.Net;

/// <summary>
/// Base class for all OpenSearch API responses. Provides transport-level diagnostics
/// (<see cref="ApiCall"/>), structured error information (<see cref="ServerError"/>),
/// and validity checking (<see cref="IsValid"/>).
/// </summary>
public abstract class OpenSearchResponse
{
	/// <summary>
	/// Transport-level details about the API call (HTTP method, URI, status code,
	/// timing, audit trail, request/response bodies).
	/// </summary>
	[JsonIgnore]
	public ApiCallDetails? ApiCall { get; internal set; }

	/// <summary>
	/// The structured server error, populated when OpenSearch returns an error response (4xx/5xx).
	/// </summary>
	[JsonIgnore]
	public ServerError? ServerError { get; internal set; }

	/// <summary>
	/// Whether this response represents a successful API call.
	/// Returns <c>true</c> when <see cref="ApiCall"/> is null (manually constructed response)
	/// or when the HTTP call succeeded and no server error was returned.
	/// </summary>
	[JsonIgnore]
	public virtual bool IsValid =>
		ApiCall is null || (ApiCall.Success && ServerError is null);

	/// <summary>
	/// The exception that occurred during the request, if any.
	/// </summary>
	[JsonIgnore]
	public Exception? OriginalException => ApiCall?.OriginalException;

	/// <summary>
	/// A human-readable diagnostic string with HTTP method, URI, status code,
	/// audit trail, and optionally request/response bodies.
	/// </summary>
	[JsonIgnore]
	public string DebugInformation =>
		ApiCall?.DebugInformation() ?? "No transport details available.";

	/// <summary>
	/// Whether a successful (2xx) response carries failures that OpenSearch would answer with
	/// 502/503/504 as a single request. The transport then marks the node dead and retries the
	/// next one, as it does for those status codes. Multi-item responses such as msearch override this.
	/// </summary>
	protected internal virtual bool HasRetryableFailure() => false;

	/// <summary>Whether <paramref name="statusCode"/> makes the transport retry on another node.</summary>
	protected static bool IsRetryableStatusCode(int statusCode) =>
		HttpClientTransport.IsRetryableStatusCode(statusCode);
}
