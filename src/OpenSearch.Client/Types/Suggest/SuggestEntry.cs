using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>A single suggest entry containing text, offset, length, and options.</summary>
public sealed class SuggestEntry<TOption>
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = default!;

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("length")]
	public int Length { get; set; }

	[JsonPropertyName("options")]
	public List<TOption> Options { get; set; } = [];
}
