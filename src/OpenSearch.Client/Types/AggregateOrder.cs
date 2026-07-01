using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The <c>order</c> of a bucket aggregation: one or more <c>{ field: asc|desc }</c> entries applied in
/// order. Build with the factory helpers, e.g. <c>AggregateOrder.By("_count", SortOrder.Desc)</c> or
/// <c>AggregateOrder.By(("total_sales", SortOrder.Desc), ("_key", SortOrder.Asc))</c>.
/// </summary>
[JsonConverter(typeof(AggregateOrderConverter))]
public sealed class AggregateOrder
{
	/// <summary>The ordered list of (field, direction) pairs.</summary>
	public IReadOnlyList<KeyValuePair<string, SortOrder>> Orders { get; }

	internal AggregateOrder(IReadOnlyList<KeyValuePair<string, SortOrder>> orders) => Orders = orders;

	public static AggregateOrder By(string field, SortOrder order) =>
		new([new KeyValuePair<string, SortOrder>(field, order)]);

	public static AggregateOrder By(params (string Field, SortOrder Order)[] orders) =>
		new(orders.Select(o => new KeyValuePair<string, SortOrder>(o.Field, o.Order)).ToArray());

	public static AggregateOrder CountAscending => By("_count", SortOrder.Asc);
	public static AggregateOrder CountDescending => By("_count", SortOrder.Desc);
	public static AggregateOrder KeyAscending => By("_key", SortOrder.Asc);
	public static AggregateOrder KeyDescending => By("_key", SortOrder.Desc);
}

public sealed class AggregateOrderConverter : JsonConverter<AggregateOrder>
{
	public override AggregateOrder? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.StartObject:
				return new AggregateOrder(ReadEntries(ref reader, options));
			case JsonTokenType.StartArray:
				var all = new List<KeyValuePair<string, SortOrder>>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
				{
					if (reader.TokenType != JsonTokenType.StartObject)
						throw new JsonException($"Unexpected token {reader.TokenType} in AggregateOrder array.");
					all.AddRange(ReadEntries(ref reader, options));
				}
				return new AggregateOrder(all);
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for AggregateOrder.");
		}
	}

	private static List<KeyValuePair<string, SortOrder>> ReadEntries(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		var entries = new List<KeyValuePair<string, SortOrder>>();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			var field = reader.GetString()!;
			reader.Read();
			var order = JsonSerializer.Deserialize<SortOrder>(ref reader, options);
			entries.Add(new KeyValuePair<string, SortOrder>(field, order));
		}
		return entries;
	}

	public override void Write(Utf8JsonWriter writer, AggregateOrder value, JsonSerializerOptions options)
	{
		if (value.Orders.Count == 1)
		{
			WriteEntry(writer, value.Orders[0], options);
			return;
		}

		writer.WriteStartArray();
		foreach (var entry in value.Orders)
			WriteEntry(writer, entry, options);
		writer.WriteEndArray();
	}

	private static void WriteEntry(Utf8JsonWriter writer, KeyValuePair<string, SortOrder> entry, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(entry.Key);
		JsonSerializer.Serialize(writer, entry.Value, options);
		writer.WriteEndObject();
	}
}
