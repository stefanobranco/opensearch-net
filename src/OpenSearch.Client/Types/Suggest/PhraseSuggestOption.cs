using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>An option from a phrase suggester.</summary>
public sealed class PhraseSuggestOption
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = default!;

	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("highlighted")]
	public string? Highlighted { get; set; }
}
