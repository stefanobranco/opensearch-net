using System.Text;

namespace OpenSearch.Net;

/// <summary>
/// Extension methods for retrieving <see cref="ApiCallDetails"/> from response objects
/// and formatting NEST-style debug information.
/// </summary>
public static class ApiCallDetailsExtensions
{
	/// <summary>
	/// Retrieves the <see cref="ApiCallDetails"/> attached to a response object by the transport,
	/// or <c>null</c> if none exist (e.g., for manually constructed responses).
	/// Prefers the <see cref="OpenSearchResponse.ApiCall"/> property when available,
	/// falling back to the <see cref="ApiCallDetailsStore"/> side-channel.
	/// </summary>
	public static ApiCallDetails? GetApiCallDetails<T>(this T response) where T : class =>
		response is OpenSearchResponse osResponse ? osResponse.ApiCall : ApiCallDetailsStore.Get(response);

	/// <summary>
	/// Builds a NEST-style diagnostic string from the call details, including HTTP method, URI,
	/// status code, audit trail, and optionally request/response bodies.
	/// </summary>
	public static string DebugInformation(this ApiCallDetails details)
	{
		ArgumentNullException.ThrowIfNull(details);

		var sb = new StringBuilder();

		// Status line
		var successLabel = details.Success ? "successful" : "unsuccessful";
		sb.Append(details.Success ? "Valid" : "Invalid");
		sb.Append($" OpenSearch response built from a {successLabel} call on ");
		sb.Append(details.HttpMethod.ToString().ToUpperInvariant());
		sb.Append(": ");
		sb.AppendLine(details.Uri?.PathAndQuery ?? "/");

		// Audit trail
		if (details.AuditTrail is { Events.Count: > 0 } trail)
		{
			sb.AppendLine("# Audit trail:");
			for (var i = 0; i < trail.Events.Count; i++)
			{
				var evt = trail.Events[i];
				sb.Append($" - [{i + 1}] {evt.Type}:");
				if (evt.Node is not null)
					sb.Append($" Node: {evt.Node.Host}");
				if (evt.Duration is not null)
					sb.Append($" Took: {evt.Duration.Value}");
				if (evt.StatusCode is not null)
					sb.Append($" Status: {evt.StatusCode}");
				if (evt.Exception is not null)
					sb.Append($" Exception: {evt.Exception.Message}");
				sb.AppendLine();
			}
		}

		// Request body
		if (details.RequestBodyBytes is { Length: > 0 } reqBytes)
		{
			sb.AppendLine("# Request:");
			sb.AppendLine(Encoding.UTF8.GetString(reqBytes));
		}

		// Response body
		if (details.ResponseBodyBytes is { Length: > 0 } respBytes)
		{
			sb.AppendLine("# Response:");
			sb.AppendLine(Encoding.UTF8.GetString(respBytes));
		}

		// Original exception
		if (details.OriginalException is not null)
		{
			sb.AppendLine("# OriginalException:");
			sb.AppendLine(details.OriginalException.ToString());
		}

		return sb.ToString();
	}
}
