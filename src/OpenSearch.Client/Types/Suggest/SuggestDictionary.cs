using System.Text.Json;

namespace OpenSearch.Client;

/// <summary>
/// Wraps the raw suggest response dictionary and provides typed accessors
/// for term, phrase, and completion suggest results.
/// </summary>
public sealed class SuggestDictionary<TDocument>
{
	private static readonly JsonSerializerOptions s_options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
	};

	private readonly Dictionary<string, List<JsonElement>>? _raw;

	public SuggestDictionary(Dictionary<string, List<JsonElement>>? raw) => _raw = raw;

	/// <summary>Retrieves term suggest entries by name.</summary>
	public IReadOnlyList<SuggestEntry<TermSuggestOption>>? GetTerm(string name)
		=> Deserialize<SuggestEntry<TermSuggestOption>>(name);

	/// <summary>Retrieves phrase suggest entries by name.</summary>
	public IReadOnlyList<SuggestEntry<PhraseSuggestOption>>? GetPhrase(string name)
		=> Deserialize<SuggestEntry<PhraseSuggestOption>>(name);

	/// <summary>Retrieves completion suggest entries by name.</summary>
	public IReadOnlyList<SuggestEntry<CompletionSuggestOption<TDocument>>>? GetCompletion(string name)
		=> Deserialize<SuggestEntry<CompletionSuggestOption<TDocument>>>(name);

	private IReadOnlyList<T>? Deserialize<T>(string name)
	{
		if (_raw is null || !_raw.TryGetValue(name, out var elements))
			return null;

		var result = new List<T>(elements.Count);
		foreach (var el in elements)
		{
			var item = JsonSerializer.Deserialize<T>(el.GetRawText(), s_options);
			if (item is not null)
				result.Add(item);
		}
		return result;
	}
}
