using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for metric aggregations — serialized as <c>{ "&lt;kind&gt;": { ... } }</c>.
/// Ported from the aggregation coverage of the elasticsearch-net client and the OpenSearch docs,
/// adapted to this client's <see cref="AggregationContainer"/> API. Each asserts the wire shape plus
/// a serialize→deserialize→serialize round-trip through the production serializer.
/// </summary>
public class MetricAggregationSerializationTests : AggregationSerializationTestBase
{
	[Fact]
	public void Avg_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Avg(new AverageAggregation
		{
			Field = "number_of_commits",
			Missing = Element(10.0),
			Script = Script.Inline("_value * 1.2"),
		}), "avg");

		body.GetProperty("field").GetString().Should().Be("number_of_commits");
		body.GetProperty("missing").GetDouble().Should().Be(10.0);
		body.GetProperty("script").GetProperty("source").GetString().Should().Be("_value * 1.2");
	}

	[Fact]
	public void Min_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Min(new MinAggregation { Field = "last_activity", Format = "yyyy" }), "min");
		body.GetProperty("field").GetString().Should().Be("last_activity");
		body.GetProperty("format").GetString().Should().Be("yyyy");
	}

	[Fact]
	public void Max_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Max(new MaxAggregation { Field = "number_of_commits" }), "max");
		body.GetProperty("field").GetString().Should().Be("number_of_commits");
	}

	[Fact]
	public void Sum_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Sum(new SumAggregation { Field = "number_of_commits" }), "sum");
		body.GetProperty("field").GetString().Should().Be("number_of_commits");
	}

	[Fact]
	public void ValueCount_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.ValueCount(new ValueCountAggregation { Field = "state" }), "value_count");
		body.GetProperty("field").GetString().Should().Be("state");
	}

	[Fact]
	public void Stats_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Stats(new StatsAggregation { Field = "number_of_commits" }), "stats");
		body.GetProperty("field").GetString().Should().Be("number_of_commits");
	}

	[Fact]
	public void ExtendedStats_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.ExtendedStats(new ExtendedStatsAggregation { Field = "grade", Sigma = 3.0 }), "extended_stats");
		body.GetProperty("field").GetString().Should().Be("grade");
		body.GetProperty("sigma").GetDouble().Should().Be(3.0);
	}

	[Fact]
	public void Cardinality_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Cardinality(new CardinalityAggregation
		{
			Field = "state",
			PrecisionThreshold = 100,
		}), "cardinality");

		body.GetProperty("field").GetString().Should().Be("state");
		body.GetProperty("precision_threshold").GetInt32().Should().Be(100);
	}

	[Fact]
	public void Percentiles_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Percentiles(new PercentilesAggregation
		{
			Field = "load_time",
			Percents = [95.0, 99.0, 99.9],
			Keyed = false,
		}), "percentiles");

		body.GetProperty("field").GetString().Should().Be("load_time");
		body.GetProperty("percents").GetArrayLength().Should().Be(3);
		body.GetProperty("percents")[2].GetDouble().Should().Be(99.9);
	}

	[Fact]
	public void PercentileRanks_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.PercentileRanks(new PercentileRanksAggregation
		{
			Field = "load_time",
			Values = [500.0, 600.0],
		}), "percentile_ranks");

		body.GetProperty("field").GetString().Should().Be("load_time");
		body.GetProperty("values")[0].GetDouble().Should().Be(500.0);
	}

	[Fact]
	public void MedianAbsoluteDeviation_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MedianAbsoluteDeviation(new MedianAbsoluteDeviationAggregation
		{
			Field = "rating",
			Compression = 100.0,
		}), "median_absolute_deviation");

		body.GetProperty("field").GetString().Should().Be("rating");
		body.GetProperty("compression").GetDouble().Should().Be(100.0);
	}

	[Fact]
	public void Boxplot_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.Boxplot(new BoxplotAggregation { Field = "load_time", Compression = 100.0 }), "boxplot");
		body.GetProperty("field").GetString().Should().Be("load_time");
		body.GetProperty("compression").GetDouble().Should().Be(100.0);
	}

	[Fact]
	public void MatrixStats_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.MatrixStats(new MatrixStatsAggregation { Fields = ["poverty", "income"] }), "matrix_stats");
		body.GetProperty("fields").GetArrayLength().Should().Be(2);
		body.GetProperty("fields")[0].GetString().Should().Be("poverty");
	}

	[Fact]
	public void WeightedAvg_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.WeightedAvg(new WeightedAverageAggregation
		{
			Value = new WeightedAverageValue { Field = "grade" },
			Weight = new WeightedAverageValue { Field = "weight" },
		}), "weighted_avg");

		body.GetProperty("value").GetProperty("field").GetString().Should().Be("grade");
		body.GetProperty("weight").GetProperty("field").GetString().Should().Be("weight");
	}

	[Fact]
	public void ScriptedMetric_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.ScriptedMetric(new ScriptedMetricAggregation
		{
			InitScript = Script.Inline("state.transactions = []"),
			MapScript = Script.Inline("state.transactions.add(doc.type.value == 'sale' ? doc.amount.value : -1 * doc.amount.value)"),
			CombineScript = Script.Inline("double profit = 0; for (t in state.transactions) { profit += t } return profit"),
			ReduceScript = Script.Inline("double profit = 0; for (a in states) { profit += a } return profit"),
		}), "scripted_metric");

		body.GetProperty("init_script").GetProperty("source").GetString().Should().Be("state.transactions = []");
		body.GetProperty("reduce_script").GetProperty("source").GetString().Should().StartWith("double profit");
	}

	[Fact]
	public void TopHits_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.TopHits(new TopHitsAggregation { Size = 3, From = 0, TrackScores = true }), "top_hits");
		body.GetProperty("size").GetInt32().Should().Be(3);
		body.GetProperty("track_scores").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void GeoCentroid_serializes_and_round_trips()
	{
		var body = AggBody(AggregationContainer.GeoCentroid(new GeoCentroidAggregation { Field = "location" }), "geo_centroid");
		body.GetProperty("field").GetString().Should().Be("location");
	}
}
