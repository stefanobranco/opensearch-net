using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a range aggregation.</summary>
public sealed class RangeBucket
{
	[JsonPropertyName("key")]
	public string? Key { get; set; }

	[JsonPropertyName("from")]
	public double? From { get; set; }

	[JsonPropertyName("to")]
	public double? To { get; set; }

	[JsonPropertyName("from_as_string")]
	public string? FromAsString { get; set; }

	[JsonPropertyName("to_as_string")]
	public string? ToAsString { get; set; }

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
