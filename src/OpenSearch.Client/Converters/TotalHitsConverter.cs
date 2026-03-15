using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Converters;

/// <summary>
/// Reads <see cref="TotalHits"/> from either the object form
/// <c>{ "value": N, "relation": "eq" }</c> or a bare integer <c>N</c>.
/// Writes the canonical object form.
/// </summary>
public sealed class TotalHitsConverter : JsonConverter<TotalHits>
{
	public override TotalHits? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Number)
		{
			return new TotalHits { Value = reader.GetInt64(), Relation = "eq" };
		}

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			long value = 0;
			string relation = "eq";

			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType != JsonTokenType.PropertyName) continue;
				var prop = reader.GetString();
				reader.Read();

				switch (prop)
				{
					case "value":
						value = reader.GetInt64();
						break;
					case "relation":
						relation = reader.GetString() ?? "eq";
						break;
				}
			}

			return new TotalHits { Value = value, Relation = relation };
		}

		if (reader.TokenType == JsonTokenType.Null)
			return null;

		throw new JsonException($"Unexpected token {reader.TokenType} when reading TotalHits");
	}

	public override void Write(Utf8JsonWriter writer, TotalHits value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("value", value.Value);
		writer.WriteString("relation", value.Relation);
		writer.WriteEndObject();
	}
}
