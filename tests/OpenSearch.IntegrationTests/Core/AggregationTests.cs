using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using OpenSearch.Client.Indices;
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
						new TermQuery { Value = JsonSerializer.SerializeToElement("electronics") }),
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
