using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Serializes/deserializes <see cref="Field"/> as a plain JSON string.
/// </summary>
public sealed class FieldConverter : JsonConverter<Field>
{
	public override Field? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var str = reader.GetString();
		return str is not null ? new Field(str) : null;
	}

	public override void Write(Utf8JsonWriter writer, Field value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Name);
	}
}
