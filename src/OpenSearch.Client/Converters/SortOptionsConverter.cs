using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Custom converter for <see cref="SortOptions"/>. Handles the polymorphic sort wire format:
/// <list type="bullet">
/// <item>String → FieldSort with that field name</item>
/// <item>Object with "_score" → ScoreSort</item>
/// <item>Object with "_doc" → ScoreSort (Doc variant)</item>
/// <item>Object with "_geo_distance" → GeoDistanceSort</item>
/// <item>Object with "_script" → ScriptSort</item>
/// <item>Object with any other key → FieldSort with that key as field name</item>
/// </list>
/// </summary>
public sealed class SortOptionsConverter : JsonConverter<SortOptions>
{
	public override SortOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType == JsonTokenType.String)
		{
			var fieldName = reader.GetString()!;
			return SortOptions.Field(new FieldSort { FieldName = fieldName });
		}

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected string or object for SortOptions, got {reader.TokenType}.");

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		foreach (var prop in root.EnumerateObject())
		{
			return prop.Name switch
			{
				"_score" => SortOptions.Score(JsonSerializer.Deserialize<ScoreSort>(prop.Value, options)),
				"_doc" => SortOptions.Doc(JsonSerializer.Deserialize<ScoreSort>(prop.Value, options)),
				"_geo_distance" => SortOptions.GeoDistance(JsonSerializer.Deserialize<GeoDistanceSort>(prop.Value, options)!),
				"_script" => SortOptions.Script(JsonSerializer.Deserialize<ScriptSort>(prop.Value, options)!),
				_ => ReadFieldSort(prop.Name, prop.Value, options)
			};
		}

		throw new JsonException("Empty object in SortOptions.");
	}

	private static SortOptions ReadFieldSort(string fieldName, JsonElement value, JsonSerializerOptions options)
	{
		if (value.ValueKind == JsonValueKind.String)
		{
			// Shorthand: {"price": "asc"} → FieldSort with order
			var orderStr = value.GetString();
			SortOrder? order = orderStr switch
			{
				"asc" => SortOrder.Asc,
				"desc" => SortOrder.Desc,
				_ => null
			};
			return SortOptions.Field(new FieldSort { FieldName = fieldName, Order = order });
		}

		if (value.ValueKind == JsonValueKind.Object)
		{
			// Delegate to STJ — FieldName is [JsonIgnore] so it won't interfere
			var sort = JsonSerializer.Deserialize<FieldSort>(value, options)!;
			sort.FieldName = fieldName;
			return SortOptions.Field(sort);
		}

		return SortOptions.Field(new FieldSort { FieldName = fieldName });
	}

	public override void Write(Utf8JsonWriter writer, SortOptions value, JsonSerializerOptions options)
	{
		var (key, variantValue) = value.VariantKind switch
		{
			SortOptions.SortKind.Score => ("_score", value.Variant),
			SortOptions.SortKind.Doc => ("_doc", value.Variant),
			SortOptions.SortKind.GeoDistance => ("_geo_distance", value.Variant),
			SortOptions.SortKind.Script => ("_script", value.Variant),
			SortOptions.SortKind.Field => (((FieldSort)value.Variant).FieldName, value.Variant),
			_ => throw new JsonException($"Unknown SortKind: {value.VariantKind}")
		};

		writer.WriteStartObject();
		writer.WritePropertyName(key);
		JsonSerializer.Serialize(writer, variantValue, variantValue.GetType(), options);
		writer.WriteEndObject();
	}
}
