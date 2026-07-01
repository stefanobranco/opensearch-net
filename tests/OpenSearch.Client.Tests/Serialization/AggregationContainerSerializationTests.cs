using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Fixtures for <see cref="AggregationContainer"/>-level concerns that cut across all aggregation
/// kinds: the <c>meta</c> sibling, the <c>aggregations</c>/<c>aggs</c> sub-aggregation keys, and
/// deep nesting — all round-tripped through the production serializer.
/// </summary>
public class AggregationContainerSerializationTests : AggregationSerializationTestBase
{
	[Fact]
	public void Meta_and_aggregations_serialize_as_siblings_and_round_trip()
	{
		var agg = AggregationContainer.Terms(new TermsAggregationFields { Field = "status" });
		agg.Meta = new Dictionary<string, object> { ["color"] = "blue" };
		agg.Aggregations = new Dictionary<string, AggregationContainer>
		{
			["avg_price"] = AggregationContainer.Avg(new AverageAggregation { Field = "price" }),
		};

		var root = AssertRoundTrips(agg);
		root.GetProperty("terms").GetProperty("field").GetString().Should().Be("status");
		root.GetProperty("meta").GetProperty("color").GetString().Should().Be("blue");
		root.GetProperty("aggregations").GetProperty("avg_price")
			.GetProperty("avg").GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void Aggs_alias_serializes_as_aggs_key()
	{
		var agg = AggregationContainer.Filter(QueryContainer.Term("category", new TermQuery { Value = Element("books") }));
		agg.Aggs = new Dictionary<string, AggregationContainer>
		{
			["count"] = AggregationContainer.ValueCount(new ValueCountAggregation { Field = "id" }),
		};

		var root = AssertRoundTrips(agg);
		root.TryGetProperty("aggs", out var aggs).Should().BeTrue();
		aggs.GetProperty("count").GetProperty("value_count").GetProperty("field").GetString().Should().Be("id");
	}

	[Fact]
	public void Deeply_nested_aggregations_round_trip()
	{
		// terms → date_histogram → sum, three levels deep.
		var monthly = AggregationContainer.DateHistogram(new DateHistogramAggregationFields
		{
			Field = "created",
			CalendarInterval = "month",
		});
		monthly.Aggregations = new Dictionary<string, AggregationContainer>
		{
			["revenue"] = AggregationContainer.Sum(new SumAggregation { Field = "amount" }),
		};

		var byStatus = AggregationContainer.Terms(new TermsAggregationFields { Field = "status" });
		byStatus.Aggregations = new Dictionary<string, AggregationContainer> { ["monthly"] = monthly };

		var root = AssertRoundTrips(byStatus);
		var inner = root.GetProperty("aggregations").GetProperty("monthly");
		inner.GetProperty("date_histogram").GetProperty("calendar_interval").GetString().Should().Be("month");
		inner.GetProperty("aggregations").GetProperty("revenue")
			.GetProperty("sum").GetProperty("field").GetString().Should().Be("amount");
	}
}
