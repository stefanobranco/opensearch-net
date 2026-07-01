using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A geo-bounding box, expressible in four forms: top/bottom/left/right coordinates
/// (<see cref="CoordsGeoBounds"/>), top-left/bottom-right points (<see cref="TopLeftBottomRightGeoBounds"/>),
/// top-right/bottom-left points (<see cref="TopRightBottomLeftGeoBounds"/>), or a WKT string
/// (<see cref="WktGeoBounds"/>). Construct implicitly from any of these.
/// </summary>
[JsonConverter(typeof(GeoBoundsConverter))]
public sealed class GeoBounds
{
	public CoordsGeoBounds? Coords { get; }
	public TopLeftBottomRightGeoBounds? TopLeftBottomRight { get; }
	public TopRightBottomLeftGeoBounds? TopRightBottomLeft { get; }
	public WktGeoBounds? Wkt { get; }

	private GeoBounds(CoordsGeoBounds coords) => Coords = coords;
	private GeoBounds(TopLeftBottomRightGeoBounds tlbr) => TopLeftBottomRight = tlbr;
	private GeoBounds(TopRightBottomLeftGeoBounds trbl) => TopRightBottomLeft = trbl;
	private GeoBounds(WktGeoBounds wkt) => Wkt = wkt;

	public static implicit operator GeoBounds(CoordsGeoBounds coords) => new(coords);
	public static implicit operator GeoBounds(TopLeftBottomRightGeoBounds tlbr) => new(tlbr);
	public static implicit operator GeoBounds(TopRightBottomLeftGeoBounds trbl) => new(trbl);
	public static implicit operator GeoBounds(WktGeoBounds wkt) => new(wkt);
}

public sealed class GeoBoundsConverter : JsonConverter<GeoBounds>
{
	public override GeoBounds? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Unexpected token {reader.TokenType} for GeoBounds.");

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.TryGetProperty("wkt", out _))
			return root.Deserialize<WktGeoBounds>(options)!;
		if (root.TryGetProperty("top_left", out _) || root.TryGetProperty("bottom_right", out _))
			return root.Deserialize<TopLeftBottomRightGeoBounds>(options)!;
		if (root.TryGetProperty("top_right", out _) || root.TryGetProperty("bottom_left", out _))
			return root.Deserialize<TopRightBottomLeftGeoBounds>(options)!;
		return root.Deserialize<CoordsGeoBounds>(options)!;
	}

	public override void Write(Utf8JsonWriter writer, GeoBounds value, JsonSerializerOptions options)
	{
		if (value.Wkt is not null)
			JsonSerializer.Serialize(writer, value.Wkt, options);
		else if (value.TopLeftBottomRight is not null)
			JsonSerializer.Serialize(writer, value.TopLeftBottomRight, options);
		else if (value.TopRightBottomLeft is not null)
			JsonSerializer.Serialize(writer, value.TopRightBottomLeft, options);
		else
			JsonSerializer.Serialize(writer, value.Coords, options);
	}
}
