using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Reads a JSON scalar (string, number, or boolean) into a <see cref="FieldValue"/> and writes it back
/// as the corresponding bare JSON scalar. Integral numbers round-trip as <see cref="long"/>, other
/// numbers as <see cref="double"/>.
/// </summary>
public sealed class FieldValueConverter : JsonConverter<FieldValue>
{
	public override FieldValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType switch
		{
			JsonTokenType.Null => FieldValue.Null,
			JsonTokenType.True => FieldValue.True,
			JsonTokenType.False => FieldValue.False,
			JsonTokenType.String => FieldValue.String(reader.GetString()!),
			JsonTokenType.Number => reader.TryGetInt64(out var l) ? FieldValue.Long(l) : FieldValue.Double(reader.GetDouble()),
			_ => throw new JsonException($"Unexpected token {reader.TokenType} for FieldValue."),
		};

	public override void Write(Utf8JsonWriter writer, FieldValue value, JsonSerializerOptions options)
	{
		switch (value.Kind)
		{
			case FieldValue.ValueKind.Null:
				writer.WriteNullValue();
				break;
			case FieldValue.ValueKind.Boolean:
				writer.WriteBooleanValue((bool)value.Value!);
				break;
			case FieldValue.ValueKind.Long:
				writer.WriteNumberValue((long)value.Value!);
				break;
			case FieldValue.ValueKind.Double:
				writer.WriteNumberValue((double)value.Value!);
				break;
			case FieldValue.ValueKind.String:
				writer.WriteStringValue((string)value.Value!);
				break;
			default:
				throw new JsonException($"Unknown FieldValue kind '{value.Kind}'.");
		}
	}
}
