using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A geographic location. OpenSearch accepts multiple formats:
/// <list type="bullet">
///   <item><c>{"lat": 40.7, "lon": -74.0}</c> — lat/lon object</item>
///   <item><c>"drm3btev3e86"</c> — geohash string</item>
///   <item><c>[-74.0, 40.7]</c> — GeoJSON [lon, lat] array</item>
///   <item><c>"40.7,-74.0"</c> — "lat,lon" string</item>
/// </list>
/// </summary>
[JsonConverter(typeof(Converters.GeoLocationConverter))]
public sealed class GeoLocation
{
	public double Lat { get; set; }
	public double Lon { get; set; }

	/// <summary>Geohash representation (set when deserialized from a geohash string).</summary>
	[JsonIgnore]
	public string? GeoHash { get; set; }

	public static GeoLocation FromLatLon(double lat, double lon) => new() { Lat = lat, Lon = lon };
	public static GeoLocation FromGeoHash(string geoHash) => new() { GeoHash = geoHash };
}
