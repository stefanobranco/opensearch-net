using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Pins expression-based field selection on aggregation descriptors — the consumer-reported
/// gap where <c>DateHistogram</c>/<c>Histogram</c> could only take field-name strings.
/// </summary>
public class AggregationExpressionTests
{
	private sealed class Asset
	{
		public string? Category { get; set; }
		public double Price { get; set; }
		public DateTime PublicationDate { get; set; }
	}

	private static JsonElement Serialize(SearchRequest request)
	{
		var json = JsonSerializer.Serialize(request, OpenSearchJsonOptions.RequestSerialization);
		return JsonDocument.Parse(json).RootElement;
	}

	[Fact]
	public void DateHistogram_FieldByExpression()
	{
		SearchRequest request = new SearchRequestDescriptor<Asset>()
			.Aggregations(a => a
				.DateHistogram("per_month", d => d
					.Field(x => x.PublicationDate)
					.CalendarInterval("month")));

		var aggs = Serialize(request).GetProperty("aggregations");
		var dh = aggs.GetProperty("per_month").GetProperty("date_histogram");
		dh.GetProperty("field").GetString().Should().Be("publication_date");
		dh.GetProperty("calendar_interval").GetString().Should().Be("month");
	}

	[Fact]
	public void Histogram_FieldByExpression()
	{
		SearchRequest request = new SearchRequestDescriptor<Asset>()
			.Aggregations(a => a
				.Histogram("by_price", h => h
					.Field(x => x.Price)
					.Interval(10)));

		var histogram = Serialize(request).GetProperty("aggregations")
			.GetProperty("by_price").GetProperty("histogram");
		histogram.GetProperty("field").GetString().Should().Be("price");
		histogram.GetProperty("interval").GetDouble().Should().Be(10);
	}

	[Fact]
	public void Terms_FieldByExpression_WithSubAggregations()
	{
		SearchRequest request = new SearchRequestDescriptor<Asset>()
			.Aggregations(a => a
				.Terms("by_category", t => t.Field(x => x.Category!).Size(20),
					sub => sub.Avg("avg_price", avg => avg.Field(x => x.Price))));

		var byCategory = Serialize(request).GetProperty("aggregations").GetProperty("by_category");
		byCategory.GetProperty("terms").GetProperty("field").GetString().Should().Be("category");
		byCategory.GetProperty("aggregations").GetProperty("avg_price")
			.GetProperty("avg").GetProperty("field").GetString().Should().Be("price");
	}

	[Fact]
	public void FieldNameStrings_StillSupported()
	{
		SearchRequest request = new SearchRequestDescriptor<Asset>()
			.Aggregations(a => a
				.Terms("by_category", t => t.Field("category.keyword")));

		Serialize(request).GetProperty("aggregations").GetProperty("by_category")
			.GetProperty("terms").GetProperty("field").GetString().Should().Be("category.keyword");
	}

	[Fact]
	public void FilterAggregation_ThreadsDocumentTypeIntoQuery()
	{
		SearchRequest request = new SearchRequestDescriptor<Asset>()
			.Aggregations(a => a
				.Filter("cheap", q => q.Range(x => x.Price, r => r.Lt(20)),
					sub => sub.Max("max_price", m => m.Field(x => x.Price))));

		var filter = Serialize(request).GetProperty("aggregations").GetProperty("cheap");
		filter.GetProperty("filter").GetProperty("range").GetProperty("price")
			.GetProperty("lt").GetDouble().Should().Be(20);
	}
}
