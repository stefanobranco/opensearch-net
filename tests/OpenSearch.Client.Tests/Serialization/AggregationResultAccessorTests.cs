using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Proves every fluent aggregation builder has a readable result: realistic response JSON is
/// deserialized through the production serializer and read back through the typed
/// <see cref="AggregateDictionary"/> accessors — no raw JsonElement digging. Exists because the
/// client once shipped builders for aggregations whose results could not be read.
/// </summary>
public class AggregationResultAccessorTests : SerializationTestBase
{
	private sealed class Doc { public string? Name { get; set; } }

	private static AggregateDictionary Aggs(string aggregationsJson)
	{
		var json = $$"""{"took":1,"timed_out":false,"aggregations":{{aggregationsJson}}}""";
		var response = Deserialize<SearchResponse<Doc>>(json)!;
		return new AggregateDictionary(response.Aggregations);
	}

	[Fact]
	public void Percentiles_keyed_map_form()
	{
		var aggs = Aggs("""{"load_time":{"values":{"50.0":3.5,"95.0":10.0,"99.0":null,"50.0_as_string":"3.5ms"}}}""");

		var result = aggs.Percentiles("load_time")!;
		result.Percentile(50.0).Should().Be(3.5);
		result.Percentile(95.0).Should().Be(10.0);
		result.Percentile(99.0).Should().BeNull("no data for that percentile");
		result.PercentileAsString(50.0).Should().Be("3.5ms");
		result.Values.Should().HaveCount(3);
	}

	[Fact]
	public void Percentiles_array_form_with_keyed_false()
	{
		var aggs = Aggs("""{"load_time":{"values":[{"key":"50.0","value":3.5},{"key":"95.0","value":10.0,"value_as_string":"10ms"}]}}""");

		var result = aggs.Percentiles("load_time")!;
		result.Percentile(50.0).Should().Be(3.5);
		result.PercentileAsString(95.0).Should().Be("10ms");
	}

	[Fact]
	public void Percentiles_bucket_pipeline_reads_same_shape()
	{
		var aggs = Aggs("""{"p_monthly_sales":{"values":{"50.0":123.0}}}""");

		aggs.PercentilesBucket("p_monthly_sales")!.Percentile(50.0).Should().Be(123.0);
	}

	[Fact]
	public void Filters_named_form_returns_keyed_buckets_with_sub_aggregations()
	{
		var aggs = Aggs("""
			{"messages":{"buckets":{
				"errors":{"doc_count":3,"avg_size":{"value":12.5}},
				"warnings":{"doc_count":7}
			}}}
			""");

		var result = aggs.Filters("messages")!;
		result.Named.Should().HaveCount(2);
		result.Bucket("errors")!.DocCount.Should().Be(3);
		result.Bucket("errors")!.Average("avg_size").Should().Be(12.5);
		result.Bucket("warnings")!.DocCount.Should().Be(7);
		result.Anonymous.Should().BeNull();
	}

	[Fact]
	public void Filters_anonymous_form_returns_ordered_buckets()
	{
		var aggs = Aggs("""{"messages":{"buckets":[{"doc_count":3},{"doc_count":7}]}}""");

		var result = aggs.Filters("messages")!;
		result.Anonymous.Should().HaveCount(2);
		result.Anonymous![0].DocCount.Should().Be(3);
		result.Anonymous[1].DocCount.Should().Be(7);
		result.Named.Should().BeNull();
	}

	[Fact]
	public void GeoBounds_returns_typed_bounds()
	{
		var aggs = Aggs("""
			{"viewport":{"bounds":{
				"top_left":{"lat":48.86,"lon":2.32},
				"bottom_right":{"lat":48.85,"lon":2.33}
			}}}
			""");

		var bounds = aggs.GeoBounds("viewport")!;
		bounds.TopLeftBottomRight.Should().NotBeNull();
		bounds.TopLeftBottomRight!.TopLeft!.Lat.Should().Be(48.86);
		bounds.TopLeftBottomRight.BottomRight!.Lon.Should().Be(2.33);
	}

	[Fact]
	public void GeoBounds_is_null_when_no_data()
	{
		var aggs = Aggs("""{"viewport":{}}""");

		aggs.GeoBounds("viewport").Should().BeNull();
	}

	[Fact]
	public void MaxBucket_returns_value_and_winning_keys()
	{
		var aggs = Aggs("""{"best_month":{"value":550.0,"keys":["2024-03","2024-07"]}}""");

		var result = aggs.MaxBucket("best_month")!;
		result.Value.Should().Be(550.0);
		result.Keys.Should().Equal("2024-03", "2024-07");
	}

	[Fact]
	public void MinBucket_and_value_pipelines_read_their_shapes()
	{
		var aggs = Aggs("""
			{
				"worst_month":{"value":10.0,"keys":["2024-01"]},
				"total_sales":{"value":9999.0},
				"avg_sales":{"value":833.25}
			}
			""");

		aggs.MinBucket("worst_month")!.Keys.Should().Equal("2024-01");
		aggs.SumBucket("total_sales").Should().Be(9999.0);
		aggs.AvgBucket("avg_sales").Should().Be(833.25);
	}

	[Fact]
	public void StatsBucket_and_extended_stats_bucket_read_stats_shapes()
	{
		var aggs = Aggs("""
			{
				"sales_stats":{"count":3,"min":10.0,"max":50.0,"avg":30.0,"sum":90.0},
				"sales_ext":{"count":3,"min":10.0,"max":50.0,"avg":30.0,"sum":90.0,"sum_of_squares":3400.0,"variance":266.7,"std_deviation":16.3}
			}
			""");

		var stats = aggs.StatsBucket("sales_stats")!;
		stats.Count.Should().Be(3);
		stats.Avg.Should().Be(30.0);

		var ext = aggs.ExtendedStatsBucket("sales_ext")!;
		ext.StdDeviation.Should().Be(16.3);
		ext.SumOfSquares.Should().Be(3400.0);
	}

	[Fact]
	public void Accessors_strip_typed_keys_prefixes()
	{
		var aggs = Aggs("""
			{
				"tdigest_percentiles#load_time":{"values":{"50.0":3.5}},
				"filters#messages":{"buckets":{"errors":{"doc_count":3}}},
				"geo_bounds#viewport":{"bounds":{"top_left":{"lat":1.0,"lon":2.0},"bottom_right":{"lat":0.5,"lon":2.5}}},
				"max_bucket#best":{"value":5.0,"keys":["a"]}
			}
			""");

		aggs.Percentiles("load_time")!.Percentile(50.0).Should().Be(3.5);
		aggs.Filters("messages")!.Bucket("errors")!.DocCount.Should().Be(3);
		aggs.GeoBounds("viewport").Should().NotBeNull();
		aggs.MaxBucket("best")!.Value.Should().Be(5.0);
	}
}
