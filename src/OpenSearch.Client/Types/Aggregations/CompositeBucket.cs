using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a composite aggregation.</summary>
public sealed class CompositeBucket
{
	[JsonPropertyName("key")]
	public Dictionary<string, JsonElement> Key { get; set; } = new();

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
