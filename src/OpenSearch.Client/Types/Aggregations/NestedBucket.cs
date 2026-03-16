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

	/// <summary>Returns a nested terms sub-aggregation.</summary>
	public BucketAggregate<TermsBucket>? Terms(string name) => Aggregations?.Terms(name);

	/// <summary>Returns a nested filter sub-aggregation.</summary>
	public FilterBucket? Filter(string name) => Aggregations?.Filter(name);

	/// <summary>Returns a nested average metric.</summary>
	public double? Average(string name) => Aggregations?.Average(name);

	/// <summary>Returns a nested sum metric.</summary>
	public double? Sum(string name) => Aggregations?.Sum(name);
}
