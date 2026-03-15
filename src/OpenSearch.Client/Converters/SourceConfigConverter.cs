using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Custom converter for <see cref="SourceConfig"/>.
/// Reads/writes either a boolean or a SourceFilter object.
/// </summary>
public sealed class SourceConfigConverter : JsonConverter<SourceConfig>
{
	public override SourceConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
			return SourceConfig.Enabled(reader.GetBoolean());

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			var filter = JsonSerializer.Deserialize<SourceFilter>(ref reader, options)!;
			return SourceConfig.Filter(filter);
		}

		throw new JsonException($"Expected bool or object for SourceConfig, got {reader.TokenType}.");
	}

	public override void Write(Utf8JsonWriter writer, SourceConfig value, JsonSerializerOptions options)
	{
		if (value.IsBool)
			writer.WriteBooleanValue(value.AsBool());
		else
			JsonSerializer.Serialize(writer, value.AsFilter(), options);
	}
}
