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
}
