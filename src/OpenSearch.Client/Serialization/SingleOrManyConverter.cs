using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Applied by generated code to <c>List&lt;T&gt;</c> properties whose wire form may be either a
/// single item or an array (spec single-or-array unions, e.g. a policy's <c>ism_template</c>).
/// Reads both forms; always writes the array form, which the server accepts everywhere.
/// </summary>
public sealed class SingleOrManyConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(List<>);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter)Activator.CreateInstance(
			typeof(SingleOrManyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}

internal sealed class SingleOrManyConverter<T> : JsonConverter<List<T>>
{
	public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var list = new List<T>();
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
				list.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
			return list;
		}

		return [JsonSerializer.Deserialize<T>(ref reader, options)!];
	}

	public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (var item in value)
			JsonSerializer.Serialize(writer, item, options);
		writer.WriteEndArray();
	}
}
