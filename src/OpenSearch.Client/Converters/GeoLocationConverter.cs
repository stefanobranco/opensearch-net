using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client.Converters;

/// <summary>
/// Reads GeoLocation from lat/lon objects, "lat,lon" strings, geohash strings, or [lon, lat] arrays.
/// Writes as <c>{"lat": ..., "lon": ...}</c>.
/// </summary>
public sealed class GeoLocationConverter : JsonConverter<GeoLocation>
{
	public override GeoLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.StartObject:
				return ReadObject(ref reader);
			case JsonTokenType.String:
				return ReadString(reader.GetString()!);
			case JsonTokenType.StartArray:
				return ReadArray(ref reader);
			case JsonTokenType.Null:
				return null;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for GeoLocation.");
		}
	}

	public override void Write(Utf8JsonWriter writer, GeoLocation value, JsonSerializerOptions options)
	{
		if (value.GeoHash is not null)
		{
			writer.WriteStringValue(value.GeoHash);
			return;
		}

		writer.WriteStartObject();
		writer.WriteNumber("lat", value.Lat);
		writer.WriteNumber("lon", value.Lon);
		writer.WriteEndObject();
	}

	private static GeoLocation ReadObject(ref Utf8JsonReader reader)
	{
		double lat = 0, lon = 0;
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName) continue;
			var prop = reader.GetString();
			reader.Read();
			switch (prop)
			{
				case "lat": lat = reader.GetDouble(); break;
				case "lon": lon = reader.GetDouble(); break;
				default: reader.Skip(); break;
			}
		}
		return new GeoLocation { Lat = lat, Lon = lon };
	}

	private static GeoLocation ReadString(string value)
	{
		// Try "lat,lon" format
		var comma = value.IndexOf(',');
		if (comma > 0
			&& double.TryParse(value.AsSpan(0, comma), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
			&& double.TryParse(value.AsSpan(comma + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
		{
			return new GeoLocation { Lat = lat, Lon = lon };
		}
		// Treat as geohash
		return GeoLocation.FromGeoHash(value);
	}

	private static GeoLocation ReadArray(ref Utf8JsonReader reader)
	{
		// GeoJSON: [lon, lat]
		reader.Read();
		var lon = reader.GetDouble();
		reader.Read();
		var lat = reader.GetDouble();
		// Skip any remaining elements and closing bracket
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) { }
		return new GeoLocation { Lat = lat, Lon = lon };
	}
}
