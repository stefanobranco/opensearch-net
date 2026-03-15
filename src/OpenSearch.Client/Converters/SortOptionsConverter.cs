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
		// The value is the FieldSort properties object (order, mode, nested, etc.)
		// We need to deserialize it and set the FieldName
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
			// Full object: {"price": {"order": "asc", "mode": "avg"}}
			// Deserialize as generated FieldSort (without FieldName) then copy properties
			var sort = new FieldSort { FieldName = fieldName };

			if (value.TryGetProperty("order", out var orderEl))
				sort.Order = JsonSerializer.Deserialize<SortOrder>(orderEl, options);
			if (value.TryGetProperty("mode", out var modeEl))
				sort.Mode = modeEl.GetString();
			if (value.TryGetProperty("missing", out var missingEl))
				sort.Missing = missingEl;
			if (value.TryGetProperty("nested", out var nestedEl))
				sort.Nested = JsonSerializer.Deserialize<NestedSortValue>(nestedEl, options);
			if (value.TryGetProperty("unmapped_type", out var unmappedEl))
				sort.UnmappedType = JsonSerializer.Deserialize<FieldType>(unmappedEl, options);
			if (value.TryGetProperty("numeric_type", out var numericEl))
				sort.NumericType = numericEl.GetString();

			return SortOptions.Field(sort);
		}

		return SortOptions.Field(new FieldSort { FieldName = fieldName });
	}

	public override void Write(Utf8JsonWriter writer, SortOptions value, JsonSerializerOptions options)
	{
		switch (value.VariantKind)
		{
			case SortOptions.SortKind.Field:
				var fieldSort = (FieldSort)value.Variant;
				writer.WriteStartObject();
				writer.WritePropertyName(fieldSort.FieldName);
				WriteFieldSortValue(writer, fieldSort, options);
				writer.WriteEndObject();
				break;

			case SortOptions.SortKind.Score:
				writer.WriteStartObject();
				writer.WritePropertyName("_score");
				JsonSerializer.Serialize(writer, (ScoreSort)value.Variant, options);
				writer.WriteEndObject();
				break;

			case SortOptions.SortKind.Doc:
				writer.WriteStartObject();
				writer.WritePropertyName("_doc");
				JsonSerializer.Serialize(writer, (ScoreSort)value.Variant, options);
				writer.WriteEndObject();
				break;

			case SortOptions.SortKind.GeoDistance:
				writer.WriteStartObject();
				writer.WritePropertyName("_geo_distance");
				JsonSerializer.Serialize(writer, (GeoDistanceSort)value.Variant, options);
				writer.WriteEndObject();
				break;

			case SortOptions.SortKind.Script:
				writer.WriteStartObject();
				writer.WritePropertyName("_script");
				JsonSerializer.Serialize(writer, (ScriptSort)value.Variant, options);
				writer.WriteEndObject();
				break;

			default:
				throw new JsonException($"Unknown SortKind: {value.VariantKind}");
		}
	}

	private static void WriteFieldSortValue(Utf8JsonWriter writer, FieldSort sort, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		if (sort.Order is not null)
		{
			writer.WritePropertyName("order");
			JsonSerializer.Serialize(writer, sort.Order, options);
		}
		if (sort.Mode is not null)
			writer.WriteString("mode", sort.Mode);
		if (sort.Missing is not null)
		{
			writer.WritePropertyName("missing");
			sort.Missing.Value.WriteTo(writer);
		}
		if (sort.Nested is not null)
		{
			writer.WritePropertyName("nested");
			JsonSerializer.Serialize(writer, sort.Nested, options);
		}
		if (sort.UnmappedType is not null)
		{
			writer.WritePropertyName("unmapped_type");
			JsonSerializer.Serialize(writer, sort.UnmappedType, options);
		}
		if (sort.NumericType is not null)
			writer.WriteString("numeric_type", sort.NumericType);
		writer.WriteEndObject();
	}
}
