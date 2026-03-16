using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>Request for the msearch_template API. Items are serialized as NDJSON (header + body pairs).</summary>
public sealed class MsearchTemplateRequest
{
	/// <summary>Default index for searches that don't specify one.</summary>
	[JsonIgnore]
	public string? Index { get; set; }

	/// <summary>When true, minimizes network round-trips for cross-cluster search requests.</summary>
	[JsonIgnore]
	public bool? CcsMinimizeRoundtrips { get; set; }

	/// <summary>Maximum number of concurrent searches the API can execute.</summary>
	[JsonIgnore]
	public int? MaxConcurrentSearches { get; set; }

	/// <summary>When true, the total hits count is returned as an integer.</summary>
	[JsonIgnore]
	public bool? RestTotalHitsAsInt { get; set; }

	/// <summary>The type of search to perform (query_then_fetch, dfs_query_then_fetch).</summary>
	[JsonIgnore]
	public string? SearchType { get; set; }

	/// <summary>When true, aggregation and suggester names are prefixed by their type in the response.</summary>
	[JsonIgnore]
	public bool? TypedKeys { get; set; }

	/// <summary>The search template operations to execute.</summary>
	[JsonIgnore]
	public List<MsearchTemplateItem> SearchTemplates { get; set; } = [];
}

/// <summary>A single search template within a multi-search template request (header + body pair).</summary>
public sealed class MsearchTemplateItem
{
	public MsearchHeader Header { get; set; } = new();

	public TemplateConfig Body { get; set; } = new();
}

/// <summary>Configuration for a search template — either a stored template (Id) or an inline template (Source).</summary>
public sealed class TemplateConfig
{
	/// <summary>The ID of a stored search template.</summary>
	[JsonPropertyName("id")]
	public string? Id { get; set; }

	/// <summary>An inline search template (Mustache format).</summary>
	[JsonPropertyName("source")]
	public JsonElement? Source { get; set; }

	/// <summary>Parameters to substitute into the template.</summary>
	[JsonPropertyName("params")]
	public Dictionary<string, JsonElement>? Params { get; set; }
}
