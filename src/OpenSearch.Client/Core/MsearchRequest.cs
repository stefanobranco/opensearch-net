using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>
/// Request for the msearch API. Contains a list of <see cref="MsearchItem"/> items
/// that are serialized as NDJSON (header + body pairs).
/// </summary>
public sealed class MsearchRequest
{
	/// <summary>Default index for searches that don't specify one.</summary>
	[JsonIgnore]
	public string? Index { get; set; }

	/// <summary>
	/// When true, returns partial results if there are shard request timeouts or shard failures.
	/// </summary>
	[JsonIgnore]
	public bool? AllowPartialResults { get; set; }

	/// <summary>
	/// When true, network round-trips between the coordinating node and remote clusters are minimized for cross-cluster search requests.
	/// </summary>
	[JsonIgnore]
	public bool? CcsMinimizeRoundtrips { get; set; }

	/// <summary>Maximum number of concurrent searches the msearch API can execute.</summary>
	[JsonIgnore]
	public int? MaxConcurrentSearches { get; set; }

	/// <summary>Maximum number of concurrent shard requests that each sub-search executes per node.</summary>
	[JsonIgnore]
	public int? MaxConcurrentShardRequests { get; set; }

	/// <summary>Threshold for enforcing a pre-filter round-trip to prefilter search shards.</summary>
	[JsonIgnore]
	public int? PreFilterShardSize { get; set; }

	/// <summary>When true, the total hits count is returned as an integer.</summary>
	[JsonIgnore]
	public bool? RestTotalHitsAsInt { get; set; }

	/// <summary>The type of search to perform (query_then_fetch, dfs_query_then_fetch).</summary>
	[JsonIgnore]
	public string? SearchType { get; set; }

	/// <summary>When true, aggregation and suggester names are prefixed by their type in the response.</summary>
	[JsonIgnore]
	public bool? TypedKeys { get; set; }

	/// <summary>The search operations to execute.</summary>
	[JsonIgnore]
	public List<MsearchItem> Searches { get; set; } = [];
}

/// <summary>
/// A single search within a multi-search request.
/// Each item has a header (routing metadata) and a body (search request fields).
/// </summary>
public sealed class MsearchItem
{
	/// <summary>The header specifying target index, routing, and other metadata.</summary>
	public MsearchHeader Header { get; set; } = new();

	/// <summary>The search body containing query, size, sort, etc.</summary>
	public MsearchBody Body { get; set; } = new();
}

/// <summary>
/// The header line for a multi-search item. Specifies target indices and routing options.
/// </summary>
public sealed class MsearchHeader
{
	[JsonPropertyName("index")]
	public string? Index { get; set; }

	[JsonPropertyName("allow_no_indices")]
	public bool? AllowNoIndices { get; set; }

	[JsonPropertyName("expand_wildcards")]
	public string? ExpandWildcards { get; set; }

	[JsonPropertyName("ignore_unavailable")]
	public bool? IgnoreUnavailable { get; set; }

	[JsonPropertyName("preference")]
	public string? Preference { get; set; }

	[JsonPropertyName("request_cache")]
	public bool? RequestCache { get; set; }

	[JsonPropertyName("routing")]
	public string? Routing { get; set; }

	[JsonPropertyName("search_type")]
	public string? SearchType { get; set; }

	[JsonPropertyName("ccs_minimize_roundtrips")]
	public bool? CcsMinimizeRoundtrips { get; set; }

	[JsonPropertyName("allow_partial_search_results")]
	public bool? AllowPartialSearchResults { get; set; }

	[JsonPropertyName("ignore_throttled")]
	public bool? IgnoreThrottled { get; set; }
}

/// <summary>
/// The body of a multi-search item. Contains the search query and options.
/// </summary>
public sealed class MsearchBody
{
	[JsonPropertyName("query")]
	public JsonElement? Query { get; set; }

	[JsonPropertyName("from")]
	public int? From { get; set; }

	[JsonPropertyName("size")]
	public int? Size { get; set; }

	[JsonPropertyName("sort")]
	public JsonElement? Sort { get; set; }

	[JsonPropertyName("_source")]
	public JsonElement? Source { get; set; }

	[JsonPropertyName("aggregations")]
	public JsonElement? Aggregations { get; set; }

	[JsonPropertyName("highlight")]
	public JsonElement? Highlight { get; set; }

	[JsonPropertyName("collapse")]
	public JsonElement? Collapse { get; set; }

	[JsonPropertyName("post_filter")]
	public JsonElement? PostFilter { get; set; }

	[JsonPropertyName("explain")]
	public bool? Explain { get; set; }

	[JsonPropertyName("stored_fields")]
	public JsonElement? StoredFields { get; set; }

	[JsonPropertyName("docvalue_fields")]
	public JsonElement? DocvalueFields { get; set; }

	[JsonPropertyName("script_fields")]
	public JsonElement? ScriptFields { get; set; }

	[JsonPropertyName("knn")]
	public JsonElement? Knn { get; set; }

	[JsonPropertyName("fields")]
	public JsonElement? Fields { get; set; }

	[JsonPropertyName("min_score")]
	public double? MinScore { get; set; }

	[JsonPropertyName("profile")]
	public bool? Profile { get; set; }

	[JsonPropertyName("rescore")]
	public JsonElement? Rescore { get; set; }

	[JsonPropertyName("search_after")]
	public JsonElement? SearchAfter { get; set; }

	[JsonPropertyName("stats")]
	public List<string>? Stats { get; set; }

	[JsonPropertyName("terminate_after")]
	public int? TerminateAfter { get; set; }

	[JsonPropertyName("timeout")]
	public string? Timeout { get; set; }

	[JsonPropertyName("track_scores")]
	public bool? TrackScores { get; set; }

	[JsonPropertyName("track_total_hits")]
	public JsonElement? TrackTotalHits { get; set; }

	[JsonPropertyName("version")]
	public bool? Version { get; set; }

	[JsonPropertyName("seq_no_primary_term")]
	public bool? SeqNoPrimaryTerm { get; set; }

	[JsonPropertyName("suggest")]
	public JsonElement? Suggest { get; set; }

	[JsonPropertyName("indices_boost")]
	public JsonElement? IndicesBoost { get; set; }

	[JsonPropertyName("pit")]
	public JsonElement? Pit { get; set; }

	[JsonPropertyName("ext")]
	public JsonElement? Ext { get; set; }
}
