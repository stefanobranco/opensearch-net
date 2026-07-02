using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A hit's <c>matched_queries</c>: an array of query names, or — when the search sets
/// <c>include_named_queries_score</c> — a map of query name to score.
/// </summary>
[JsonConverter(typeof(MatchedQueriesConverter))]
public sealed class MatchedQueries
{
	private MatchedQueries(IReadOnlyList<string>? names, IReadOnlyDictionary<string, double>? scores)
	{
		_names = names;
		Scores = scores;
	}

	private readonly IReadOnlyList<string>? _names;

	/// <summary>The names of the matched queries, regardless of wire form.</summary>
	public IReadOnlyList<string> Names => _names ?? Scores!.Keys.ToList();

	/// <summary>Per-query scores, or <c>null</c> when the plain name-array form was returned.</summary>
	public IReadOnlyDictionary<string, double>? Scores { get; }

	public static MatchedQueries OfNames(IReadOnlyList<string> names) => new(names, null);
	public static MatchedQueries OfScores(IReadOnlyDictionary<string, double> scores) => new(null, scores);
}

public sealed class MatchedQueriesConverter : JsonConverter<MatchedQueries>
{
	public override MatchedQueries Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.StartArray:
				var names = new List<string>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					names.Add(reader.GetString()!);
				return MatchedQueries.OfNames(names);
			case JsonTokenType.StartObject:
				var scores = new Dictionary<string, double>(StringComparer.Ordinal);
				while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
				{
					var name = reader.GetString()!;
					reader.Read();
					scores[name] = reader.GetDouble();
				}
				return MatchedQueries.OfScores(scores);
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for MatchedQueries.");
		}
	}

	public override void Write(Utf8JsonWriter writer, MatchedQueries value, JsonSerializerOptions options)
	{
		if (value.Scores is { } scores)
		{
			writer.WriteStartObject();
			foreach (var (name, score) in scores)
				writer.WriteNumber(name, score);
			writer.WriteEndObject();
			return;
		}
		writer.WriteStartArray();
		foreach (var name in value.Names)
			writer.WriteStringValue(name);
		writer.WriteEndArray();
	}
}
