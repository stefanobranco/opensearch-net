using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a global aggregation.</summary>
public sealed class GlobalBucket : IBucketWithSubAggregations
{
	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
