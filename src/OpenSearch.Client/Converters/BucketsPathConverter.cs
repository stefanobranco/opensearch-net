using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Serializes <see cref="BucketsPath"/> as one of its three wire forms — a string, an array of
/// strings, or a string→string object — and reads any of those back.
/// </summary>
public sealed class BucketsPathConverter : JsonConverter<BucketsPath>
{
	public override BucketsPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return BucketsPath.Path(reader.GetString()!);
			case JsonTokenType.StartArray:
			{
				var paths = new List<string>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					paths.Add(reader.GetString()!);
				return BucketsPath.Paths([.. paths]);
			}
			case JsonTokenType.StartObject:
			{
				var named = new Dictionary<string, string>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
				{
					var key = reader.GetString()!;
					reader.Read();
					named[key] = reader.GetString()!;
				}
				return BucketsPath.NamedPaths(named);
			}
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for BucketsPath.");
		}
	}

	public override void Write(Utf8JsonWriter writer, BucketsPath value, JsonSerializerOptions options)
	{
		if (value.Named is not null)
		{
			writer.WriteStartObject();
			foreach (var (key, path) in value.Named)
				writer.WriteString(key, path);
			writer.WriteEndObject();
		}
		else if (value.Multiple is not null)
		{
			writer.WriteStartArray();
			foreach (var path in value.Multiple)
				writer.WriteStringValue(path);
			writer.WriteEndArray();
		}
		else
		{
			writer.WriteStringValue(value.Single);
		}
	}
}
