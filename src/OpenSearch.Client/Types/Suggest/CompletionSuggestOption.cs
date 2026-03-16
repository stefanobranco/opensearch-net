using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>An option from a completion suggester.</summary>
public sealed class CompletionSuggestOption<TDocument>
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = default!;

	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("_source")]
	public TDocument? Source { get; set; }

	[JsonPropertyName("_index")]
	public string? Index { get; set; }

	[JsonPropertyName("_id")]
	public string? Id { get; set; }
}
