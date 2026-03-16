using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

/// <summary>
/// A single document result from a multi-get response.
/// Unlike <see cref="Hit{TDocument}"/> (used in search), mget results include
/// a <see cref="Found"/> field indicating whether the document exists.
/// </summary>
public sealed class MgetHit<TDocument>
{
	public bool Found { get; set; }

	[JsonPropertyName("_index")]
	public string? Index { get; set; }

	[JsonPropertyName("_id")]
	public string? Id { get; set; }

	[JsonPropertyName("_source")]
	public TDocument? Source { get; set; }

	[JsonPropertyName("_version")]
	public long? Version { get; set; }

	[JsonPropertyName("_seq_no")]
	public long? SeqNo { get; set; }

	[JsonPropertyName("_primary_term")]
	public long? PrimaryTerm { get; set; }

	[JsonPropertyName("_routing")]
	public string? Routing { get; set; }

	public Dictionary<string, object>? Fields { get; set; }
}
