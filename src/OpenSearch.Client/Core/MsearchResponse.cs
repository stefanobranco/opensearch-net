using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>Response from the msearch API.</summary>
public sealed class MsearchResponse
{
	/// <summary>How long the operation took, in milliseconds.</summary>
	public long Took { get; set; }

	/// <summary>The list of search responses, one per search in the request.</summary>
	public List<MsearchResponseItem>? Responses { get; set; }
}

/// <summary>A single search response within a multi-search response. On failure, <see cref="Error"/> is populated instead of <see cref="Hits"/>.</summary>
public sealed class MsearchResponseItem
{
	public int? Status { get; set; }

	public JsonElement? Error { get; set; }

	public long? Took { get; set; }

	[JsonPropertyName("timed_out")]
	public bool? TimedOut { get; set; }

	[JsonPropertyName("_shards")]
	public JsonElement? Shards { get; set; }

	public MsearchHitsMetadata? Hits { get; set; }

	public JsonElement? Aggregations { get; set; }

	public JsonElement? Suggest { get; set; }
}

/// <summary>Hits metadata in a multi-search response item (non-generic since sub-searches can target different types).</summary>
public sealed class MsearchHitsMetadata
{
	public JsonElement? Total { get; set; }

	[JsonPropertyName("max_score")]
	public double? MaxScore { get; set; }

	public List<JsonElement>? Hits { get; set; }
}
