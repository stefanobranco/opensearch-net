using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A two-dimensional Cartesian point (OpenSearch 2.4+), expressible as an <c>{x, y}</c> object
/// (<see cref="XyCartesianCoordinates"/>), an <c>[x, y]</c> array, or an <c>"x,y"</c>/WKT string.
/// Construct implicitly from any of these.
/// </summary>
[JsonConverter(typeof(XyLocationConverter))]
public sealed class XyLocation
{
	public XyCartesianCoordinates? Cartesian { get; }
	public IReadOnlyList<double>? Coords { get; }
	public string? Text { get; }

	private XyLocation(XyCartesianCoordinates cartesian) => Cartesian = cartesian;
	private XyLocation(IReadOnlyList<double> coords) => Coords = coords;
	private XyLocation(string text) => Text = text;

	public static implicit operator XyLocation(XyCartesianCoordinates cartesian) => new(cartesian);
	public static implicit operator XyLocation(double[] coords) => new(coords);
	public static implicit operator XyLocation(string text) => new(text);
}

public sealed class XyLocationConverter : JsonConverter<XyLocation>
{
	public override XyLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return reader.GetString()!;
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<double[]>(ref reader, options)!;
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<XyCartesianCoordinates>(ref reader, options)
					?? throw new JsonException("Failed to read XyCartesianCoordinates.");
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for XyLocation.");
		}
	}

	public override void Write(Utf8JsonWriter writer, XyLocation value, JsonSerializerOptions options)
	{
		if (value.Text is not null)
			writer.WriteStringValue(value.Text);
		else if (value.Coords is not null)
			JsonSerializer.Serialize(writer, value.Coords, options);
		else
			JsonSerializer.Serialize(writer, value.Cartesian, options);
	}
}
