using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a histogram aggregation.</summary>
public sealed class HistogramBucket : IBucketWithSubAggregations
{
	[JsonPropertyName("key")]
	public double Key { get; set; }

	[JsonPropertyName("key_as_string")]
	public string? KeyAsString { get; set; }

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }

	// ── Sub-aggregation convenience accessors ──

	public BucketAggregate<TermsBucket>? Terms(string name) => Aggregations?.Terms(name);
	public FilterBucket? Filter(string name) => Aggregations?.Filter(name);
	public double? Average(string name) => Aggregations?.Average(name);
	public double? Sum(string name) => Aggregations?.Sum(name);
	public double? Min(string name) => Aggregations?.Min(name);
	public double? Max(string name) => Aggregations?.Max(name);
	public long? Cardinality(string name) => Aggregations?.Cardinality(name);
}
