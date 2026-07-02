using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A semantic field's <c>chunking</c> setting: <c>true</c>/<c>false</c> to toggle default
/// chunking, or a list of <see cref="SemanticChunkingStrategy"/> configurations.
/// </summary>
[JsonConverter(typeof(SemanticChunkingConverter))]
public sealed class SemanticChunking
{
	private SemanticChunking(bool? enabled, IReadOnlyList<SemanticChunkingStrategy>? strategies)
	{
		Enabled = enabled;
		Strategies = strategies;
	}

	/// <summary>The boolean form, or <c>null</c> when strategies are configured.</summary>
	public bool? Enabled { get; }

	/// <summary>The configured strategies, or <c>null</c> when the boolean form is used.</summary>
	public IReadOnlyList<SemanticChunkingStrategy>? Strategies { get; }

	public static SemanticChunking Enable(bool enabled) => new(enabled, null);
	public static SemanticChunking Of(params SemanticChunkingStrategy[] strategies) => new(null, strategies);

	public static implicit operator SemanticChunking(bool enabled) => Enable(enabled);
	public static implicit operator SemanticChunking(SemanticChunkingStrategy[] strategies) => new(null, strategies);
	public static implicit operator SemanticChunking(List<SemanticChunkingStrategy> strategies) => new(null, strategies);
}

public sealed class SemanticChunkingConverter : JsonConverter<SemanticChunking>
{
	public override SemanticChunking Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.True:
			case JsonTokenType.False:
				return SemanticChunking.Enable(reader.GetBoolean());
			case JsonTokenType.StartArray:
				var strategies = new List<SemanticChunkingStrategy>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					strategies.Add(JsonSerializer.Deserialize<SemanticChunkingStrategy>(ref reader, options)!);
				return strategies;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for SemanticChunking.");
		}
	}

	public override void Write(Utf8JsonWriter writer, SemanticChunking value, JsonSerializerOptions options)
	{
		if (value.Enabled is { } enabled)
		{
			writer.WriteBooleanValue(enabled);
			return;
		}
		writer.WriteStartArray();
		foreach (var strategy in value.Strategies!)
			JsonSerializer.Serialize(writer, strategy, options);
		writer.WriteEndArray();
	}
}
