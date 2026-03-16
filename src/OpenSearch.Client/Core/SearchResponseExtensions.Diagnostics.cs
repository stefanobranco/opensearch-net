using System.Text;
using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Diagnostic extension methods for search responses, providing
/// shard-failure details and rich debug information.
/// </summary>
public static class SearchResponseDiagnosticsExtensions
{
	/// <summary>
	/// Returns the first shard failure reason as a formatted string, or <c>null</c> if no failures occurred.
	/// </summary>
	public static string? ShardError<TDocument>(this SearchResponse<TDocument> response) =>
		FormatShardError(response.Shards);

	/// <summary>
	/// Returns a human-readable diagnostic string with response metadata, shard failure details,
	/// and transport-level call details (HTTP method, URI, status code, audit trail, bodies).
	/// </summary>
	public static string SearchDebugInformation<TDocument>(this SearchResponse<TDocument> response) =>
		BuildDebugInfo(response.IsValid, response.Took, response.TimedOut, response.Shards,
			response.ApiCall);

	/// <summary>
	/// Returns the first shard failure reason for a multi-search response item.
	/// </summary>
	public static string? ServerError(this MsearchResponseItem item)
	{
		if (item.Error is { } error && error.ValueKind == System.Text.Json.JsonValueKind.Object)
		{
			if (error.TryGetProperty("reason", out var reason))
				return reason.GetString();
			return error.ToString();
		}
		return null;
	}

	private static string? FormatShardError(OpenSearch.Client.Common.ShardStatistics? shards)
	{
		if (shards?.Failures is not { Count: > 0 } failures)
			return null;

		var first = failures[0];
		var reason = first.Reason;
		if (reason is null) return "Unknown shard failure";

		var sb = new StringBuilder();
		sb.Append(reason.Type ?? "error");
		if (reason.Reason is not null)
			sb.Append(": ").Append(reason.Reason);
		if (reason.CausedBy is not null)
			sb.Append(" (caused by: ").Append(reason.CausedBy.Reason ?? reason.CausedBy.Type).Append(')');
		return sb.ToString();
	}

	private static string BuildDebugInfo(
		bool isValid, long took, bool timedOut, OpenSearch.Client.Common.ShardStatistics? shards,
		ApiCallDetails? callDetails)
	{
		var sb = new StringBuilder();

		// Status line
		sb.Append(isValid ? "Valid" : "Invalid");
		sb.Append(" OpenSearch response.");

		// Timing
		sb.Append(" Took: ").Append(took).Append("ms.");
		if (timedOut)
			sb.Append(" TIMED OUT.");

		// Shard statistics
		if (shards is not null)
		{
			sb.Append(" Shards: ")
				.Append(shards.Successful).Append('/').Append(shards.Total)
				.Append(" successful");
			if (shards.Skipped is > 0)
				sb.Append(", ").Append(shards.Skipped).Append(" skipped");
			if (shards.Failed > 0)
				sb.Append(", ").Append(shards.Failed).Append(" failed");
			sb.Append('.');
		}

		// Shard failure details
		if (shards?.Failures is { Count: > 0 } failures)
		{
			sb.AppendLine();
			sb.AppendLine("# Shard failures:");
			foreach (var failure in failures)
			{
				sb.Append(" - ");
				if (failure.Index is not null)
					sb.Append('[').Append(failure.Index).Append(']');
				sb.Append("[shard ").Append(failure.Shard).Append(']');
				if (failure.Node is not null)
					sb.Append(" on node ").Append(failure.Node);
				sb.Append(": ");
				if (failure.Reason is not null)
				{
					sb.Append(failure.Reason.Type ?? "error");
					if (failure.Reason.Reason is not null)
						sb.Append(" — ").Append(failure.Reason.Reason);
				}
				else
				{
					sb.Append("unknown error");
				}
				sb.AppendLine();
			}
		}

		// Transport-level details
		if (callDetails is not null)
		{
			sb.AppendLine();
			sb.Append(callDetails.DebugInformation());
		}

		return sb.ToString();
	}
}
