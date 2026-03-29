using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Converters;

/// <summary>
/// Reads FieldAndFormat from either a plain string or an object with "field" and optional "format".
/// Writes as an object when format is present, or as a plain string when only the field is set.
/// </summary>
public sealed class FieldAndFormatConverter : JsonConverter<FieldAndFormat>
{
	public override FieldAndFormat? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
				return new FieldAndFormat { Field = reader.GetString()! };
			case JsonTokenType.StartObject:
				return ReadObject(ref reader);
			case JsonTokenType.Null:
				return null;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for FieldAndFormat.");
		}
	}

	public override void Write(Utf8JsonWriter writer, FieldAndFormat value, JsonSerializerOptions options)
	{
		if (value.Format is null)
		{
			writer.WriteStringValue(value.Field);
			return;
		}

		writer.WriteStartObject();
		writer.WriteString("field", value.Field);
		writer.WriteString("format", value.Format);
		writer.WriteEndObject();
	}

	private static FieldAndFormat ReadObject(ref Utf8JsonReader reader)
	{
		string? field = null, format = null;
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName) continue;
			var prop = reader.GetString();
			reader.Read();
			switch (prop)
			{
				case "field": field = reader.GetString(); break;
				case "format": format = reader.GetString(); break;
				default: reader.Skip(); break;
			}
		}
		return new FieldAndFormat { Field = field ?? "", Format = format };
	}
}
