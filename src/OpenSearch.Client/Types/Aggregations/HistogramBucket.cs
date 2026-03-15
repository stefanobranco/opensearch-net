using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a histogram aggregation.</summary>
public sealed class HistogramBucket
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
}
