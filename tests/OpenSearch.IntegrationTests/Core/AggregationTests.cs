using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class AggregationTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void TermsAggregation_ReturnsBuckets()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Terms("by_category", t => t.Field("category"))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue();

		var buckets = response.Aggs().Terms("by_category");
		buckets.Should().NotBeNull();
		buckets!.Count.Should().Be(2);
		buckets.Select(b => b.Key).Should().BeEquivalentTo(["electronics", "books"]);
	}

	[SkipIfNoCluster]
	public void TermsWithSubAggregation_ChainsCorrectly()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Terms("by_category", t => t.Field("category"),
					sub => sub.Avg("avg_price", avg => avg.Field("price")))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue();

		var buckets = response.Aggs().Terms("by_category");
		buckets.Should().NotBeNull();

		var electronics = buckets!.First(b => b.Key == "electronics");
		electronics.DocCount.Should().Be(2);
		electronics.Aggregations.Should().NotBeNull();
		electronics.Aggregations!.Average("avg_price").Should().Be(75.0);
	}

	[SkipIfNoCluster]
	public void FilterAggregation_WithSubAggregation()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Filter("electronics_only",
					QueryContainer.Term("category",
						new TermQuery { Value = "electronics" }),
					sub => sub.Avg("avg_price", avg => avg.Field("price")))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue();

		var filterBucket = response.Aggs().Filter("electronics_only");
		filterBucket.Should().NotBeNull();
		filterBucket!.DocCount.Should().Be(2);
		filterBucket.Average("avg_price").Should().Be(75.0);
	}

	[SkipIfNoCluster]
	public void MultipleSubAggregations_OnTermsBucket()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Terms("by_category", t => t.Field("category"),
					sub => sub
						.Avg("avg_price", avg => avg.Field("price"))
						.Max("max_price", max => max.Field("price"))
						.Min("min_price", min => min.Field("price")))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue();

		var buckets = response.Aggs().Terms("by_category");
		buckets.Should().NotBeNull();

		var electronics = buckets!.First(b => b.Key == "electronics");
		electronics.Aggregations.Should().NotBeNull();
		electronics.Aggregations!.Average("avg_price").Should().Be(75.0);
		electronics.Aggregations!.Max("max_price").Should().Be(100.0);
		electronics.Aggregations!.Min("min_price").Should().Be(50.0);

		var books = buckets.First(b => b.Key == "books");
		books.DocCount.Should().Be(3);
		books.Aggregations!.Average("avg_price").Should().BeApproximately(16.63, 0.01);
	}

	[SkipIfNoCluster]
	public void MetricAggregations_ViaAggsDictionary()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Avg("avg_price", avg => avg.Field("price"))
				.Min("min_price", min => min.Field("price"))
				.Max("max_price", max => max.Field("price"))
				.Cardinality("unique_categories", c => c.Field("category"))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue();

		var aggs = response.Aggs();
		aggs.Min("min_price").Should().BeApproximately(9.99, 0.01);
		aggs.Max("max_price").Should().Be(100.0);
		aggs.Average("avg_price").Should().NotBeNull();
		aggs.Cardinality("unique_categories").Should().Be(2);
	}

	[SkipIfNoCluster]
	public void PercentilesAggregation_ReturnsTypedPercentiles()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Percentiles("price_percentiles", p => p.Field("price").Percents([50.0, 99.0]))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue(response.DebugInformation);

		var percentiles = response.Aggs().Percentiles("price_percentiles");
		percentiles.Should().NotBeNull();
		percentiles!.Percentile(50.0).Should().NotBeNull();
		percentiles.Values.Should().HaveCount(2);
	}

	[SkipIfNoCluster]
	public void FiltersAggregation_NamedBuckets_WithSubAggregation()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Filters("by_kind", f => f.Filters(new Dictionary<string, QueryContainer>
					{
						["cheap"] = QueryContainer.Range("price", new RangeQuery { Lt = 20 }),
						["expensive"] = QueryContainer.Range("price", new RangeQuery { Gte = 20 }),
					}),
					sub => sub.Avg("avg_price", avg => avg.Field("price")))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue(response.DebugInformation);

		var filters = response.Aggs().Filters("by_kind");
		filters.Should().NotBeNull();
		filters!.Bucket("cheap")!.DocCount.Should().Be(2);
		filters.Bucket("expensive")!.DocCount.Should().Be(3);
		filters.Bucket("expensive")!.Average("avg_price").Should().BeApproximately(58.31, 0.01);
	}

	[SkipIfNoCluster]
	public void HistogramWithMaxBucketPipeline_ReturnsWinningBucketKeys()
	{
		var index = SetupTestData();

		var request = new SearchRequest
		{
			Index = [index],
			Size = 0,
			Aggregations = new Dictionary<string, AggregationContainer>
			{
				["by_price"] = WithSubAggs(
					AggregationContainer.Histogram(new HistogramAggregationFields { Field = "price", Interval = 50 }),
					new Dictionary<string, AggregationContainer>
					{
						["total"] = AggregationContainer.Sum(new SumAggregation { Field = "price" }),
					}),
				["best_range"] = AggregationContainer.MaxBucket(new MaxBucketAggregation
				{
					BucketsPath = "by_price>total",
				}),
			},
		};

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue(response.DebugInformation);

		// Histogram buckets by 50: [0,50)=9.99+14.99+24.92=49.90, [50,100)=50, [100,150)=100.
		var best = response.Aggs().MaxBucket("best_range");
		best.Should().NotBeNull();
		best!.Value.Should().Be(100.0, "the [100,150) bucket has the largest sum");
		best.Keys.Should().ContainSingle().Which.Should().StartWith("100");
	}

	[SkipIfNoCluster]
	public void GeoBoundsAggregation_ReturnsTypedBounds()
	{
		var index = UniqueIndex("agg-geo");

		Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { NumberOfShards = "1" },
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, Property>
				{
					["location"] = Property.GeoPointProperty(new GeoPointProperty()),
				},
			},
		});

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<GeoDoc> { Document = new() { Location = GeoLocation.FromLatLon(48.86, 2.32) }, Id = "1" },
				new BulkIndexOperation<GeoDoc> { Document = new() { Location = GeoLocation.FromLatLon(48.85, 2.35) }, Id = "2" },
			],
		});

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a.GeoBounds("viewport", g => g.Field("location")));
		request.Index = [index];

		var response = Client.Core.Search<GeoDoc>(request);
		response.IsValid.Should().BeTrue(response.DebugInformation);

		var bounds = response.Aggs().GeoBounds("viewport");
		bounds.Should().NotBeNull();
		bounds!.TopLeftBottomRight.Should().NotBeNull();
		bounds.TopLeftBottomRight!.TopLeft!.Lat.Should().BeApproximately(48.86, 0.01);
		bounds.TopLeftBottomRight.BottomRight!.Lon.Should().BeApproximately(2.35, 0.01);
	}

	[SkipIfNoCluster]
	public void RangeAggregation_ReturnsTypedRangeBuckets()
	{
		var index = SetupTestData();

		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Range("price_ranges", r => r
					.Field("price")
					.Ranges([new AggregationRange { To = 20 }, new AggregationRange { From = 20 }]))
			);
		request.Index = [index];

		var response = Client.Core.Search<ProductDoc>(request);
		response.IsValid.Should().BeTrue(response.DebugInformation);

		var ranges = response.Aggs().Range("price_ranges");
		ranges.Should().NotBeNull();
		ranges!.Count.Should().Be(2);
		ranges[0].DocCount.Should().Be(2);
		ranges[1].DocCount.Should().Be(3);
	}

	private sealed class GeoDoc
	{
		public GeoLocation? Location { get; set; }
	}

	private static AggregationContainer WithSubAggs(
		AggregationContainer container, Dictionary<string, AggregationContainer> subAggs)
	{
		container.Aggregations = subAggs;
		return container;
	}

	[SkipIfNoCluster]
	public void AggregationRequest_SerializesCorrectly()
	{
		SearchRequest request = new SearchRequestDescriptor()
			.Size(0)
			.Aggregations(a => a
				.Terms("by_category", t => t.Field("category"),
					sub => sub
						.Avg("avg_price", avg => avg.Field("price"))
						.Max("max_price", max => max.Field("price")))
			);

		var json = JsonSerializer.Serialize(request, OpenSearchJsonOptions.RequestSerialization);
		using var doc = JsonDocument.Parse(json);

		var aggs = doc.RootElement.GetProperty("aggregations");
		aggs.TryGetProperty("by_category", out var byCategory).Should().BeTrue();
		byCategory.TryGetProperty("terms", out var terms).Should().BeTrue();
		terms.GetProperty("field").GetString().Should().Be("category");
		byCategory.TryGetProperty("aggregations", out var subAggs).Should().BeTrue();
		subAggs.TryGetProperty("avg_price", out _).Should().BeTrue();
		subAggs.TryGetProperty("max_price", out _).Should().BeTrue();
	}

	private string SetupTestData()
	{
		var index = UniqueIndex("agg");

		Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { NumberOfShards = "1" },
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, Property>
				{
					["category"] = Property.KeywordProperty(new KeywordProperty()),
					["price"] = Property.FloatNumberProperty(new FloatNumberProperty()),
					["name"] = Property.TextProperty(new TextProperty()),
				}
			}
		});

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<ProductDoc> { Document = new() { Name = "Laptop", Category = "electronics", Price = 100.0 }, Id = "1" },
				new BulkIndexOperation<ProductDoc> { Document = new() { Name = "Phone", Category = "electronics", Price = 50.0 }, Id = "2" },
				new BulkIndexOperation<ProductDoc> { Document = new() { Name = "Novel", Category = "books", Price = 9.99 }, Id = "3" },
				new BulkIndexOperation<ProductDoc> { Document = new() { Name = "Textbook", Category = "books", Price = 24.92 }, Id = "4" },
				new BulkIndexOperation<ProductDoc> { Document = new() { Name = "Comic", Category = "books", Price = 14.99 }, Id = "5" },
			]
		});

		return index;
	}

	private sealed class ProductDoc
	{
		public string? Name { get; set; }
		public string? Category { get; set; }
		public double Price { get; set; }
	}
}
