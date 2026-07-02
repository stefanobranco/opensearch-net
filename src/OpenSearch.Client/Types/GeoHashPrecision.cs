using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Geohash precision: a geohash length (1–12) or a distance measure such as "1km" or "10m".
/// Construct implicitly, e.g. <c>Precision = 6</c> or <c>Precision = "10m"</c>.
/// </summary>
[JsonConverter(typeof(GeoHashPrecisionConverter))]
public readonly struct GeoHashPrecision
{
	private GeoHashPrecision(int? length, string? distance)
	{
		Length = length;
		Distance = distance;
	}

	/// <summary>The geohash-length form, or <c>null</c> when the distance form is used.</summary>
	public int? Length { get; }

	/// <summary>The distance form ("10m"), or <c>null</c> when the length form is used.</summary>
	public string? Distance { get; }

	public static implicit operator GeoHashPrecision(int length) => new(length, null);
	public static implicit operator GeoHashPrecision(string distance) => new(null, distance);

	public override string ToString() => Distance ?? Length?.ToString() ?? "";
}

public sealed class GeoHashPrecisionConverter : JsonConverter<GeoHashPrecision>
{
	public override GeoHashPrecision Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType switch
		{
			JsonTokenType.Number => reader.GetInt32(),
			JsonTokenType.String => reader.GetString()!,
			_ => throw new JsonException($"Unexpected token {reader.TokenType} for GeoHashPrecision."),
		};

	public override void Write(Utf8JsonWriter writer, GeoHashPrecision value, JsonSerializerOptions options)
	{
		if (value.Length is { } length)
			writer.WriteNumberValue(length);
		else
			writer.WriteStringValue(value.Distance ?? "");
	}
}
