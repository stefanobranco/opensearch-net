using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a date_histogram aggregation.</summary>
public sealed class DateHistogramBucket
{
	[JsonPropertyName("key")]
	public long Key { get; set; }

	[JsonPropertyName("key_as_string")]
	public string? KeyAsString { get; set; }

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
