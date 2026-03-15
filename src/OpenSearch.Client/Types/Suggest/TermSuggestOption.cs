using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>An option from a term suggester.</summary>
public sealed class TermSuggestOption
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = default!;

	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("freq")]
	public long Freq { get; set; }
}
