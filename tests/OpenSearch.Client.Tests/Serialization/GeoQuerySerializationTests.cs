using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for geo and shape queries. These carry the field key and its geometry in
/// <c>[JsonExtensionData]</c> (so the field name is a sibling of <c>validation_method</c>/<c>distance</c>
/// rather than a typed property). Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class GeoQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void GeoBoundingBox_serializes_and_round_trips()
	{
		var query = QueryContainer.GeoBoundingBox(new GeoBoundingBoxQuery
		{
			ExtensionData = new()
			{
				["pin.location"] = Element(new
				{
					top_left = new[] { -74.1, 40.73 },
					bottom_right = new[] { -71.12, 40.01 },
				}),
			},
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("geo_bounding_box", out var inner).Should().BeTrue();
		var box = inner.GetProperty("pin.location");
		box.GetProperty("top_left").GetArrayLength().Should().Be(2);
		box.GetProperty("bottom_right").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void GeoDistance_serializes_and_round_trips()
	{
		var query = QueryContainer.GeoDistance(new GeoDistanceQuery
		{
			Distance = "200km",
			ExtensionData = new() { ["pin.location"] = Element(new[] { -70.0, 40.0 }) },
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("geo_distance", out var inner).Should().BeTrue();
		inner.GetProperty("distance").GetString().Should().Be("200km");
		inner.GetProperty("pin.location").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void GeoPolygon_serializes_and_round_trips()
	{
		var query = QueryContainer.GeoPolygon(new GeoPolygonQuery
		{
			ExtensionData = new()
			{
				["person.location"] = Element(new
				{
					points = new[] { new[] { -70.0, 40.0 }, new[] { -80.0, 30.0 }, new[] { -90.0, 20.0 } },
				}),
			},
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("geo_polygon", out var inner).Should().BeTrue();
		inner.GetProperty("person.location").GetProperty("points").GetArrayLength().Should().Be(3);
	}

	[Fact]
	public void GeoShape_serializes_and_round_trips()
	{
		var query = QueryContainer.GeoShape(new GeoShapeQuery
		{
			ExtensionData = new()
			{
				["location"] = Element(new
				{
					shape = new { type = "envelope", coordinates = new[] { new[] { 13.0, 53.0 }, new[] { 14.0, 52.0 } } },
					relation = "within",
				}),
			},
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("geo_shape", out var inner).Should().BeTrue();
		var loc = inner.GetProperty("location");
		loc.GetProperty("relation").GetString().Should().Be("within");
		loc.GetProperty("shape").GetProperty("type").GetString().Should().Be("envelope");
	}

	[Fact]
	public void XyShape_serializes_and_round_trips()
	{
		var query = QueryContainer.XyShape(new XyShapeQuery
		{
			ExtensionData = new()
			{
				["geometry"] = Element(new
				{
					shape = new { type = "envelope", coordinates = new[] { new[] { 0.0, 10.0 }, new[] { 10.0, 0.0 } } },
				}),
			},
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("xy_shape", out var inner).Should().BeTrue();
		inner.GetProperty("geometry").GetProperty("shape").GetProperty("type").GetString().Should().Be("envelope");
	}

	[Fact]
	public void DistanceFeature_serializes_and_round_trips()
	{
		var query = QueryContainer.DistanceFeature(new DistanceFeatureQuery
		{
			Field = "location",
			Pivot = "1000m",
			Origin = GeoLocation.FromLatLon(40.0, -70.0),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("distance_feature", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("location");
		inner.GetProperty("pivot").GetString().Should().Be("1000m");
	}
}
