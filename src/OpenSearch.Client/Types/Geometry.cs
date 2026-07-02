using System.Text.Json;

namespace OpenSearch.Client;

/// <summary>
/// A GeoJSON geometry: any of the shape types (Point, LineString, Polygon, Multi* or Envelope)
/// or a GeometryCollection. Modeled as one flat type because GeoJSON discriminates by the
/// <c>type</c> property and <c>coordinates</c> nesting depth varies per shape.
/// </summary>
public sealed class Geometry
{
	/// <summary>The GeoJSON geometry type ("Point", "Polygon", "GeometryCollection", ...).</summary>
	public string? Type { get; set; }

	/// <summary>
	/// The shape coordinates. Nesting depth depends on <see cref="Type"/>: a position for Point,
	/// an array of positions for LineString, an array of rings for Polygon, and so on.
	/// </summary>
	public JsonElement? Coordinates { get; set; }

	/// <summary>The member geometries when <see cref="Type"/> is "GeometryCollection".</summary>
	public List<Geometry>? Geometries { get; set; }

	public static Geometry Point(double lon, double lat) => new()
	{
		Type = "Point",
		Coordinates = JsonSerializer.SerializeToElement(new[] { lon, lat }),
	};

	public static Geometry Collection(params Geometry[] geometries) => new()
	{
		Type = "GeometryCollection",
		Geometries = [.. geometries],
	};
}
