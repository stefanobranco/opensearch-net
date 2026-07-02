using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Custom converter for <see cref="SourceFilter"/>. Reads the object form ({includes, excludes})
/// as well as the shorthand single-field string and field-array forms; writes the object form.
/// The includes/excludes values themselves accept a single field or an array.
/// </summary>
public sealed class SourceFilterConverter : JsonConverter<SourceFilter>
{
	public override SourceFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return reader.GetString()!;
			case JsonTokenType.StartArray:
				return new SourceFilter { Includes = ReadFields(ref reader) };
			case JsonTokenType.StartObject:
				var filter = new SourceFilter();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
				{
					var property = reader.GetString();
					reader.Read();
					switch (property)
					{
						case "includes" or "include":
							filter.Includes = ReadFieldsValue(ref reader);
							break;
						case "excludes" or "exclude":
							filter.Excludes = ReadFieldsValue(ref reader);
							break;
						default:
							reader.Skip();
							break;
					}
				}
				return filter;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for SourceFilter.");
		}
	}

	private static List<string> ReadFieldsValue(ref Utf8JsonReader reader) =>
		reader.TokenType == JsonTokenType.StartArray
			? ReadFields(ref reader)
			: [reader.GetString()!];

	private static List<string> ReadFields(ref Utf8JsonReader reader)
	{
		var fields = new List<string>();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			fields.Add(reader.GetString()!);
		return fields;
	}

	public override void Write(Utf8JsonWriter writer, SourceFilter value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		WriteFields(writer, "includes", value.Includes);
		WriteFields(writer, "excludes", value.Excludes);
		writer.WriteEndObject();
	}

	private static void WriteFields(Utf8JsonWriter writer, string name, List<string>? fields)
	{
		if (fields is null)
			return;
		writer.WritePropertyName(name);
		writer.WriteStartArray();
		foreach (var field in fields)
			writer.WriteStringValue(field);
		writer.WriteEndArray();
	}
}
