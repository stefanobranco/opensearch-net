using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// End-to-end fixtures for the strongly-typed fluent aggregation API as users write it:
/// <c>new SearchRequestDescriptor().Aggregations(a =&gt; a.DateHistogram("by_month", h =&gt; h.Field(...)))</c>.
/// Exercises the hand-written <see cref="AggregationsDictDescriptor"/>, the generated per-aggregation
/// descriptors (incl. the newly-typed <c>date_histogram</c>/<c>histogram</c>), sub-aggregation nesting,
/// and the descriptor→container cast operators — serialized through the production serializer.
/// </summary>
public class FluentAggregationSerializationTests : SerializationTestBase
{
	/// <summary>Builds a search request via the fluent aggregation descriptor and returns its <c>aggregations</c>.</summary>
	private static JsonElement Aggs(System.Action<AggregationsDictDescriptor> build)
	{
		SearchRequest request = new SearchRequestDescriptor().Size(0).Aggregations(build);
		return Parse(Serialize(request)).GetProperty("aggregations");
	}

	[Fact]
	public void Terms_fluent_serializes()
	{
		var aggs = Aggs(a => a.Terms("by_status", t => t.Field("status").Size(10)));

		var terms = aggs.GetProperty("by_status").GetProperty("terms");
		terms.GetProperty("field").GetString().Should().Be("status");
		terms.GetProperty("size").GetInt32().Should().Be(10);
	}

	[Fact]
	public void DateHistogram_fluent_serializes()
	{
		// The headline of this change: date_histogram is reachable through the typed fluent builder.
		var aggs = Aggs(a => a.DateHistogram("projects_per_month", h => h
			.Field("started_on")
			.CalendarInterval("month")
			.MinDocCount(1)
			.Format("yyyy-MM-dd")));

		var dh = aggs.GetProperty("projects_per_month").GetProperty("date_histogram");
		dh.GetProperty("field").GetString().Should().Be("started_on");
		dh.GetProperty("calendar_interval").GetString().Should().Be("month");
		dh.GetProperty("min_doc_count").GetInt32().Should().Be(1);
		dh.GetProperty("format").GetString().Should().Be("yyyy-MM-dd");
	}

	[Fact]
	public void Histogram_fluent_serializes()
	{
		var aggs = Aggs(a => a.Histogram("prices", h => h.Field("price").Interval(50)));

		var h = aggs.GetProperty("prices").GetProperty("histogram");
		h.GetProperty("field").GetString().Should().Be("price");
		h.GetProperty("interval").GetDouble().Should().Be(50);
	}

	[Fact]
	public void DateHistogram_fluent_with_nested_sub_aggregation_serializes()
	{
		// date_histogram bucketing with a metric sub-aggregation — the canonical shape from the
		// elasticsearch-net DateHistogram usage test (bucket → aggregations → metric).
		var aggs = Aggs(a => a.DateHistogram("per_month", h => h.Field("date").CalendarInterval("month"),
			sub => sub.Sum("monthly_sales", s => s.Field("amount"))));

		var perMonth = aggs.GetProperty("per_month");
		perMonth.GetProperty("date_histogram").GetProperty("calendar_interval").GetString().Should().Be("month");
		perMonth.GetProperty("aggregations").GetProperty("monthly_sales")
			.GetProperty("sum").GetProperty("field").GetString().Should().Be("amount");
	}

	[Fact]
	public void Terms_fluent_with_metric_sub_aggregations_serializes()
	{
		var aggs = Aggs(a => a.Terms("by_status", t => t.Field("status"),
			sub => sub
				.Avg("avg_price", av => av.Field("price"))
				.Max("max_price", mx => mx.Field("price"))));

		var byStatus = aggs.GetProperty("by_status");
		byStatus.GetProperty("terms").GetProperty("field").GetString().Should().Be("status");
		var subAggs = byStatus.GetProperty("aggregations");
		subAggs.GetProperty("avg_price").GetProperty("avg").GetProperty("field").GetString().Should().Be("price");
		subAggs.GetProperty("max_price").GetProperty("max").GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void Global_fluent_with_sub_aggregation_serializes()
	{
		var aggs = Aggs(a => a.Global("all_products",
			sub => sub.Avg("avg_price_overall", av => av.Field("price"))));

		var global = aggs.GetProperty("all_products");
		global.GetProperty("global").ValueKind.Should().Be(JsonValueKind.Object);
		global.GetProperty("global").EnumerateObject().Should().BeEmpty();
		global.GetProperty("aggregations").GetProperty("avg_price_overall")
			.GetProperty("avg").GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void Filter_fluent_action_query_with_sub_aggregation_serializes()
	{
		var aggs = Aggs(a => a.Filter("electronics",
			q => q.Term("category", t => t.Value(Element("electronics"))),
			sub => sub.Avg("avg_price", av => av.Field("price"))));

		var filter = aggs.GetProperty("electronics");
		filter.GetProperty("filter").GetProperty("term").GetProperty("category").GetProperty("value").GetString().Should().Be("electronics");
		filter.GetProperty("aggregations").GetProperty("avg_price").GetProperty("avg").GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void Nested_fluent_with_sub_terms_serializes()
	{
		// nested bucket navigating into a nested field, then terms over a nested sub-field.
		var aggs = Aggs(a => a.Nested("project_tags", n => n.Path("tags"),
			sub => sub.Terms("tags", t => t.Field("tags.name"))));

		var nested = aggs.GetProperty("project_tags");
		nested.GetProperty("nested").GetProperty("path").GetString().Should().Be("tags");
		nested.GetProperty("aggregations").GetProperty("tags").GetProperty("terms")
			.GetProperty("field").GetString().Should().Be("tags.name");
	}

	[Fact]
	public void Multiple_top_level_aggregations_serialize()
	{
		var aggs = Aggs(a => a
			.Terms("by_status", t => t.Field("status"))
			.Avg("avg_price", av => av.Field("price"))
			.DateHistogram("over_time", h => h.Field("created").FixedInterval("7d")));

		aggs.EnumerateObject().Should().HaveCount(3);
		aggs.GetProperty("by_status").TryGetProperty("terms", out _).Should().BeTrue();
		aggs.GetProperty("avg_price").TryGetProperty("avg", out _).Should().BeTrue();
		aggs.GetProperty("over_time").GetProperty("date_histogram").GetProperty("fixed_interval").GetString().Should().Be("7d");
	}
}
