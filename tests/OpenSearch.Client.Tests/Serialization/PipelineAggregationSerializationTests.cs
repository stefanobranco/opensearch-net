using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for pipeline aggregations — those that consume the output of sibling/parent
/// aggregations via <c>buckets_path</c>. Each asserts the wire shape and a round-trip through the
/// production serializer.
/// </summary>
public class PipelineAggregationSerializationTests : AggregationSerializationTestBase
{
	[Fact]
	public void AvgBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.AvgBucket(new AverageBucketAggregation
		{
			BucketsPath = "sales_per_month>sales",
			GapPolicy = GapPolicy.Skip,
		}), "avg_bucket");

		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
		body.GetProperty("gap_policy").GetString().Should().Be("skip");
	}

	[Fact]
	public void SumBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.SumBucket(new SumBucketAggregation { BucketsPath = "sales_per_month>sales" }), "sum_bucket");
		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
	}

	[Fact]
	public void MinBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MinBucket(new MinBucketAggregation { BucketsPath = "sales_per_month>sales" }), "min_bucket");
		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
	}

	[Fact]
	public void MaxBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MaxBucket(new MaxBucketAggregation { BucketsPath = "sales_per_month>sales" }), "max_bucket");
		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
	}

	[Fact]
	public void StatsBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.StatsBucket(new StatsBucketAggregation { BucketsPath = "sales_per_month>sales" }), "stats_bucket");
		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
	}

	[Fact]
	public void ExtendedStatsBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.ExtendedStatsBucket(new ExtendedStatsBucketAggregation
		{
			BucketsPath = "sales_per_month>sales",
			Sigma = 3.0,
		}), "extended_stats_bucket");

		body.GetProperty("buckets_path").GetString().Should().Be("sales_per_month>sales");
		body.GetProperty("sigma").GetDouble().Should().Be(3.0);
	}

	[Fact]
	public void PercentilesBucket_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.PercentilesBucket(new PercentilesBucketAggregation
		{
			BucketsPath = "sales_per_month>sales",
			Percents = [25.0, 50.0, 75.0],
		}), "percentiles_bucket");

		body.GetProperty("percents").GetArrayLength().Should().Be(3);
	}

	[Fact]
	public void Derivative_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Derivative(new DerivativeAggregation { BucketsPath = "sales" }), "derivative");
		body.GetProperty("buckets_path").GetString().Should().Be("sales");
	}

	[Fact]
	public void CumulativeSum_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.CumulativeSum(new CumulativeSumAggregation { BucketsPath = "sales" }), "cumulative_sum");
		body.GetProperty("buckets_path").GetString().Should().Be("sales");
	}

	[Fact]
	public void CumulativeCardinality_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.CumulativeCardinality(new CumulativeCardinalityAggregation { BucketsPath = "distinct_users" }), "cumulative_cardinality");
		body.GetProperty("buckets_path").GetString().Should().Be("distinct_users");
	}

	[Fact]
	public void MovingFn_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MovingFn(new MovingFunctionAggregation
		{
			BucketsPath = "the_sum",
			Window = 10,
			Script = "MovingFunctions.unweightedAvg(values)",
		}), "moving_fn");

		body.GetProperty("buckets_path").GetString().Should().Be("the_sum");
		body.GetProperty("window").GetInt32().Should().Be(10);
		body.GetProperty("script").GetString().Should().Be("MovingFunctions.unweightedAvg(values)");
	}

	[Fact]
	public void BucketScript_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.BucketScript(new BucketScriptAggregation
		{
			BucketsPath = "total_sales",
			Script = Script.Inline("params.total / 100"),
		}), "bucket_script");

		body.GetProperty("buckets_path").GetString().Should().Be("total_sales");
		body.GetProperty("script").GetProperty("source").GetString().Should().Be("params.total / 100");
	}

	[Fact]
	public void BucketSelector_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.BucketSelector(new BucketSelectorAggregation
		{
			BucketsPath = "total_sales",
			Script = Script.Inline("params.total > 200"),
		}), "bucket_selector");

		body.GetProperty("script").GetProperty("source").GetString().Should().Be("params.total > 200");
	}

	[Fact]
	public void BucketSort_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.BucketSort(new BucketSortAggregation { From = 0, Size = 5 }), "bucket_sort");
		body.GetProperty("size").GetInt32().Should().Be(5);
		body.GetProperty("from").GetInt32().Should().Be(0);
	}

	[Fact]
	public void SerialDiff_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.SerialDiff(new SerialDifferencingAggregation
		{
			BucketsPath = "the_sum",
			Lag = 7,
		}), "serial_diff");

		body.GetProperty("buckets_path").GetString().Should().Be("the_sum");
		body.GetProperty("lag").GetInt32().Should().Be(7);
	}

	[Fact]
	public void Normalize_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Normalize(new NormalizeAggregation { BucketsPath = "sales" }), "normalize");
		body.GetProperty("buckets_path").GetString().Should().Be("sales");
	}
}
