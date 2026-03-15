using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A bucket from a significant_terms aggregation.</summary>
public sealed class SignificantTermsBucket
{
	[JsonPropertyName("key")]
	public string Key { get; set; } = default!;

	[JsonPropertyName("doc_count")]
	public long DocCount { get; set; }

	[JsonPropertyName("bg_count")]
	public long? BgCount { get; set; }

	[JsonPropertyName("score")]
	public double Score { get; set; }

	/// <summary>Sub-aggregations within this bucket.</summary>
	[JsonIgnore]
	public AggregateDictionary? Aggregations { get; set; }
}
