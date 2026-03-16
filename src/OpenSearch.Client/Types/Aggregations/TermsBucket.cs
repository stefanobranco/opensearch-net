using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a terms aggregation.</summary>
public sealed class TermsBucket
{
	[JsonPropertyName("key")]
	public string Key { get; set; } = default!;

	[JsonPropertyName("key_as_string")]
	public string? KeyAsString { get; set; }

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	[JsonPropertyName("doc_count_error_upper_bound")]
	public long? DocCountErrorUpperBound { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }

	// ── Sub-aggregation convenience accessors ──

	/// <summary>Returns a nested terms sub-aggregation.</summary>
	public BucketAggregate<TermsBucket>? Terms(string name) => Aggregations?.Terms(name);

	/// <summary>Returns a nested filter sub-aggregation.</summary>
	public FilterBucket? Filter(string name) => Aggregations?.Filter(name);

	/// <summary>Returns a nested nested sub-aggregation.</summary>
	public NestedBucket? Nested(string name) => Aggregations?.Nested(name);

	/// <summary>Returns a nested date histogram sub-aggregation.</summary>
	public BucketAggregate<DateHistogramBucket>? DateHistogram(string name) => Aggregations?.DateHistogram(name);

	/// <summary>Returns a nested average metric.</summary>
	public double? Average(string name) => Aggregations?.Average(name);

	/// <summary>Returns a nested sum metric.</summary>
	public double? Sum(string name) => Aggregations?.Sum(name);

	/// <summary>Returns a nested min metric.</summary>
	public double? Min(string name) => Aggregations?.Min(name);

	/// <summary>Returns a nested max metric.</summary>
	public double? Max(string name) => Aggregations?.Max(name);

	/// <summary>Returns a nested cardinality metric.</summary>
	public long? Cardinality(string name) => Aggregations?.Cardinality(name);
}
