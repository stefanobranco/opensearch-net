using System.Text;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Diagnostic extension methods for search responses, providing v1-compatible
/// <c>ServerError</c> and <c>DebugInformation</c> accessors.
/// </summary>
public static class SearchResponseDiagnosticsExtensions
{
	/// <summary>
	/// Returns the first shard failure reason, or <c>null</c> if no failures occurred.
	/// Mirrors v1's <c>response.ServerError</c> pattern.
	/// </summary>
	public static string? ServerError<TDocument>(this SearchResponse<TDocument> response) =>
		FormatShardError(response.Shards);

	/// <summary>
	/// Returns a human-readable diagnostic string with response metadata and shard failure details.
	/// Mirrors v1's <c>response.DebugInformation</c> pattern.
	/// <code>
	/// Valid OpenSearch response. Took: 5ms. Shards: 5/5 successful, 0 failed.
	/// </code>
	/// </summary>
	public static string DebugInformation<TDocument>(this SearchResponse<TDocument> response) =>
		BuildDebugInfo(response.IsValid(), response.Took, response.TimedOut, response.Shards);

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

	private static string? FormatShardError(ShardStatistics? shards)
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

	private static string BuildDebugInfo(bool isValid, long took, bool timedOut, ShardStatistics? shards)
	{
		var sb = new StringBuilder();

		// Status line — mirrors NEST's "Valid NEST response built from a successful..."
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

		return sb.ToString();
	}
}
