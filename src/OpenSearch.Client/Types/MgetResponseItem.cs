using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>
/// A single document result from a multi-get response.
/// Covers both the success case (GetResult) and the error case (MultiGetError)
/// from the spec's oneOf union.
/// </summary>
public sealed class MgetResponseItem
{
	[JsonPropertyName("_index")]
	public string? Index { get; set; }

	[JsonPropertyName("_id")]
	public string? Id { get; set; }

	[JsonPropertyName("_version")]
	public long? Version { get; set; }

	[JsonPropertyName("_seq_no")]
	public long? SeqNo { get; set; }

	[JsonPropertyName("_primary_term")]
	public long? PrimaryTerm { get; set; }

	public bool Found { get; set; }

	[JsonPropertyName("_source")]
	public System.Text.Json.JsonElement? Source { get; set; }

	[JsonPropertyName("_routing")]
	public string? Routing { get; set; }

	public System.Text.Json.JsonElement? Fields { get; set; }

	public System.Text.Json.JsonElement? Error { get; set; }
}
