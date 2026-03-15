using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a nested aggregation.</summary>
public sealed class NestedBucket
{
	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
