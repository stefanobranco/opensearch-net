using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Reads a JSON value as a string even when the token is a number or boolean.
/// OpenSearch terms aggregation bucket keys can be strings, numbers, or booleans
/// depending on the field type, but the <see cref="TermsBucket.Key"/> property
/// is always <c>string</c> for convenience.
/// </summary>
internal sealed class StringOrNumberConverter : JsonConverter<string>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
				return reader.GetString();
			case JsonTokenType.Number:
				if (reader.TryGetInt64(out var l)) return l.ToString();
				if (reader.TryGetDouble(out var d)) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
				return reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture);
			case JsonTokenType.True:
				return "true";
			case JsonTokenType.False:
				return "false";
			case JsonTokenType.Null:
				return null;
			default:
				throw new JsonException($"Unexpected token type {reader.TokenType} for string property");
		}
	}

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value);
	}
}
