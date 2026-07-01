using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for bucket aggregations. Includes the two aggregations that were previously
/// untyped (fell back to <c>JsonElement</c>): <c>date_histogram</c> and <c>histogram</c>, now backed
/// by real field types, plus <c>filter</c> (now typed as <see cref="QueryContainer"/>). Each asserts
/// the wire shape and a round-trip through the production serializer.
/// </summary>
public class BucketAggregationSerializationTests : AggregationSerializationTestBase
{
	[Fact]
	public void Terms_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Terms(new TermsAggregationFields
		{
			Field = "genre",
			Size = 10,
			MinDocCount = 2,
			ShardSize = 100,
		}), "terms");

		body.GetProperty("field").GetString().Should().Be("genre");
		body.GetProperty("size").GetInt32().Should().Be(10);
		body.GetProperty("min_doc_count").GetInt64().Should().Be(2);
	}

	[Fact]
	public void DateHistogram_serializes_and_round_trips()
	{
		// Previously untyped (JsonElement) — now backed by DateHistogramAggregationFields.
		var body = AggBody(AggregationContainer.DateHistogram(new DateHistogramAggregationFields
		{
			Field = "started_on",
			CalendarInterval = "month",
			MinDocCount = 2,
			Format = "yyyy-MM-dd",
		}), "date_histogram");

		body.GetProperty("field").GetString().Should().Be("started_on");
		body.GetProperty("calendar_interval").GetString().Should().Be("month");
		body.GetProperty("min_doc_count").GetInt32().Should().Be(2);
		body.GetProperty("format").GetString().Should().Be("yyyy-MM-dd");
	}

	[Fact]
	public void DateHistogram_with_fixed_interval_and_extended_bounds_serializes()
	{
		// extended_bounds exercises the previously-generic ExtendedBoundsFieldDateMath (now non-generic).
		var body = AggBody(AggregationContainer.DateHistogram(new DateHistogramAggregationFields
		{
			Field = "timestamp",
			FixedInterval = "30d",
			ExtendedBounds = new ExtendedBoundsFieldDateMath { Min = "2020-01-01", Max = "2020-12-31" },
		}), "date_histogram");

		body.GetProperty("fixed_interval").GetString().Should().Be("30d");
		body.GetProperty("extended_bounds").GetProperty("min").GetString().Should().Be("2020-01-01");
		body.GetProperty("extended_bounds").GetProperty("max").GetString().Should().Be("2020-12-31");
	}

	[Fact]
	public void Histogram_serializes_and_round_trips()
	{
		// Previously untyped (JsonElement) — now backed by HistogramAggregationFields.
		var body = AggBody(AggregationContainer.Histogram(new HistogramAggregationFields
		{
			Field = "price",
			Interval = 50.0,
			MinDocCount = 1,
			ExtendedBounds = new ExtendedBoundsDouble { Min = 0.0, Max = 500.0 },
		}), "histogram");

		body.GetProperty("field").GetString().Should().Be("price");
		body.GetProperty("interval").GetDouble().Should().Be(50.0);
		body.GetProperty("extended_bounds").GetProperty("max").GetDouble().Should().Be(500.0);
	}

	[Fact]
	public void Range_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Range(new RangeAggregationFields
		{
			Field = "age",
			Ranges =
			[
				new AggregationRange { To = "50" },
				new AggregationRange { From = "50", To = "100" },
				new AggregationRange { From = "100" },
			],
		}), "range");

		body.GetProperty("field").GetString().Should().Be("age");
		body.GetProperty("ranges").GetArrayLength().Should().Be(3);
		body.GetProperty("ranges")[1].GetProperty("from").GetString().Should().Be("50");
	}

	[Fact]
	public void DateRange_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.DateRange(new DateRangeAggregationFields
		{
			Field = "created",
			Format = "MM-yyyy",
			Ranges =
			[
				new DateRangeExpression { To = "now-10M/M" },
				new DateRangeExpression { From = "now-10M/M" },
			],
		}), "date_range");

		body.GetProperty("field").GetString().Should().Be("created");
		body.GetProperty("format").GetString().Should().Be("MM-yyyy");
		body.GetProperty("ranges")[0].GetProperty("to").GetString().Should().Be("now-10M/M");
	}

	[Fact]
	public void Filter_serializes_typed_query_and_round_trips()
	{
		// Previously untyped (JsonElement) — now typed as QueryContainer.
		var body = AggBody(AggregationContainer.Filter(
			QueryContainer.Term("category", new TermQuery { Value = Element("electronics") })), "filter");

		body.GetProperty("term").GetProperty("category").GetProperty("value").GetString().Should().Be("electronics");
	}

	[Fact]
	public void Nested_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Nested(new NestedAggregationFields { Path = "tags" }), "nested");
		body.GetProperty("path").GetString().Should().Be("tags");
	}

	[Fact]
	public void Missing_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Missing(new MissingAggregationFields { Field = "price" }), "missing");
		body.GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void Global_serializes_empty_body_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Global(Element(new { })), "global");
		body.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
		body.EnumerateObject().Should().BeEmpty();
	}

	[Fact]
	public void Composite_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Composite(new CompositeAggregationFields
		{
			Size = 10,
			Sources =
			[
				new Dictionary<string, CompositeAggregationSource>
				{
					["product"] = new() { Terms = new CompositeTermsAggregationSource { Field = "product" } },
				},
			],
		}), "composite");

		body.GetProperty("size").GetInt32().Should().Be(10);
		body.GetProperty("sources")[0].GetProperty("product").GetProperty("terms").GetProperty("field").GetString().Should().Be("product");
	}

	[Fact]
	public void SignificantTerms_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.SignificantTerms(new SignificantTermsAggregationFields
		{
			Field = "text",
			MinDocCount = 3,
			Size = 20,
		}), "significant_terms");

		body.GetProperty("field").GetString().Should().Be("text");
		body.GetProperty("min_doc_count").GetInt64().Should().Be(3);
	}

	[Fact]
	public void MultiTerms_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MultiTerms(new MultiTermsAggregationFields
		{
			Size = 10,
			Terms =
			[
				new MultiTermLookup { Field = "genre" },
				new MultiTermLookup { Field = "product" },
			],
		}), "multi_terms");

		body.GetProperty("terms").GetArrayLength().Should().Be(2);
		body.GetProperty("terms")[0].GetProperty("field").GetString().Should().Be("genre");
	}

	[Fact]
	public void Sampler_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Sampler(new SamplerAggregationFields { ShardSize = 200 }), "sampler");
		body.GetProperty("shard_size").GetInt32().Should().Be(200);
	}

	[Fact]
	public void Children_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Children(new ChildrenAggregationFields { Type = "answer" }), "children");
		body.GetProperty("type").GetString().Should().Be("answer");
	}

	[Fact]
	public void ReverseNested_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.ReverseNested(new ReverseNestedAggregationFields { Path = "tags" }), "reverse_nested");
		body.GetProperty("path").GetString().Should().Be("tags");
	}

	[Fact]
	public void AdjacencyMatrix_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.AdjacencyMatrix(new AdjacencyMatrixAggregationFields
		{
			Filters = new Dictionary<string, QueryContainer>
			{
				["grpA"] = QueryContainer.Term("accounts", new TermQuery { Value = Element("hillary") }),
				["grpB"] = QueryContainer.Term("accounts", new TermQuery { Value = Element("sidney") }),
			},
		}), "adjacency_matrix");

		body.GetProperty("filters").GetProperty("grpA").GetProperty("term").TryGetProperty("accounts", out _).Should().BeTrue();
	}

	[Fact]
	public void GeoDistance_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.GeoDistance(new GeoDistanceAggregationFields
		{
			Field = "location",
			Origin = GeoLocation.FromLatLon(52.3760, 4.894),
			Unit = "km",
			Ranges = [new AggregationRange { To = "100" }, new AggregationRange { From = "100", To = "300" }],
		}), "geo_distance");

		body.GetProperty("field").GetString().Should().Be("location");
		body.GetProperty("unit").GetString().Should().Be("km");
		body.GetProperty("ranges").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void IpRange_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.IpRange(new IpRangeAggregationFields
		{
			Field = "ip",
			Ranges =
			[
				new IpRangeAggregationRange { To = "10.0.0.5" },
				new IpRangeAggregationRange { From = "10.0.0.5" },
			],
		}), "ip_range");

		body.GetProperty("field").GetString().Should().Be("ip");
		body.GetProperty("ranges")[0].GetProperty("to").GetString().Should().Be("10.0.0.5");
	}

	[Fact]
	public void AutoDateHistogram_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.AutoDateHistogram(new AutoDateHistogramAggregationFields
		{
			Field = "date",
			Buckets = 10,
		}), "auto_date_histogram");

		body.GetProperty("field").GetString().Should().Be("date");
		body.GetProperty("buckets").GetInt32().Should().Be(10);
	}

	[Fact]
	public void RareTerms_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.RareTerms(new RareTermsAggregationFields
		{
			Field = "genre",
			MaxDocCount = 5,
		}), "rare_terms");

		body.GetProperty("field").GetString().Should().Be("genre");
		body.GetProperty("max_doc_count").GetInt64().Should().Be(5);
	}

	[Fact]
	public void GeohashGrid_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.GeohashGrid(new GeoHashGridAggregationFields
		{
			Field = "location",
			Precision = "5",
			Size = 1000,
		}), "geohash_grid");

		body.GetProperty("field").GetString().Should().Be("location");
		body.GetProperty("precision").GetString().Should().Be("5");
	}

	[Fact]
	public void GeotileGrid_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.GeotileGrid(new GeoTileGridAggregationFields
		{
			Field = "location",
			Precision = 7,
			Size = 1000,
		}), "geotile_grid");

		body.GetProperty("field").GetString().Should().Be("location");
		body.GetProperty("precision").GetDouble().Should().Be(7);
	}

	[Fact]
	public void DiversifiedSampler_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.DiversifiedSampler(new DiversifiedSamplerAggregationFields
		{
			Field = "author",
			ShardSize = 200,
			MaxDocsPerValue = 3,
		}), "diversified_sampler");

		body.GetProperty("field").GetString().Should().Be("author");
		body.GetProperty("max_docs_per_value").GetInt32().Should().Be(3);
	}
}
