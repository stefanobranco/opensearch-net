using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>
/// Response from the msearch API.
/// </summary>
public sealed class MsearchResponse
{
	/// <summary>How long the operation took, in milliseconds.</summary>
	public long Took { get; set; }

	/// <summary>The list of search responses, one per search in the request.</summary>
	public List<MsearchResponseItem>? Responses { get; set; }
}

/// <summary>
/// A single search response within a multi-search response.
/// Contains the same fields as a regular search response, plus a status code.
/// On failure, the error field is populated instead.
/// </summary>
public sealed class MsearchResponseItem
{
	/// <summary>HTTP status code for this sub-search.</summary>
	public int? Status { get; set; }

	/// <summary>Error details if this sub-search failed.</summary>
	public JsonElement? Error { get; set; }

	/// <summary>How long this sub-search took, in milliseconds.</summary>
	public long? Took { get; set; }

	[JsonPropertyName("timed_out")]
	public bool? TimedOut { get; set; }

	[JsonPropertyName("_shards")]
	public JsonElement? Shards { get; set; }

	/// <summary>The search hits.</summary>
	public MsearchHitsMetadata? Hits { get; set; }

	/// <summary>Aggregation results, if any.</summary>
	public JsonElement? Aggregations { get; set; }

	/// <summary>Suggest results, if any.</summary>
	public JsonElement? Suggest { get; set; }
}

/// <summary>
/// The hits metadata in a multi-search response item.
/// </summary>
public sealed class MsearchHitsMetadata
{
	/// <summary>Total number of matching documents.</summary>
	public JsonElement? Total { get; set; }

	[JsonPropertyName("max_score")]
	public double? MaxScore { get; set; }

	/// <summary>The actual search hits as raw JSON (non-generic since sub-searches can target different types).</summary>
	public List<JsonElement>? Hits { get; set; }
}
