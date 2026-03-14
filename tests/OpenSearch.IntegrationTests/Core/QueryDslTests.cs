using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using OpenSearch.Client.Indices;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class QueryDslTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void SearchWithMatchQuery()
	{
		var index = UniqueIndex("querydsl");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Alice Smith", Age = 30, Category = "engineering" }, Id = "1" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Bob Jones", Age = 25, Category = "marketing" }, Id = "2" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Charlie Smith", Age = 35, Category = "engineering" }, Id = "3" },
			]
		});

		// Match query via tagged union
		var searchResponse = Client.Core.Search<QueryDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
			Query = QueryContainer.Match(new Dictionary<string, JsonElement>
			{
				["category"] = JsonSerializer.SerializeToElement("engineering")
			})
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(2);
		searchResponse.Hits.Hits!.Select(h => h.Source!.Name)
			.Should().BeEquivalentTo(["Alice Smith", "Charlie Smith"]);
	}

	[SkipIfNoCluster]
	public void SearchWithTermQuery()
	{
		var index = UniqueIndex("querydsl");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest
		{
			Index = index,
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, JsonElement>
				{
					["status"] = JsonSerializer.SerializeToElement(new { type = "keyword" })
				}
			}
		});

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Active Doc", Age = 30, Status = "active" }, Id = "1" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Inactive Doc", Age = 25, Status = "inactive" }, Id = "2" },
			]
		});

		// Term query via tagged union
		var searchResponse = Client.Core.Search<QueryDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
			Query = QueryContainer.Term(new Dictionary<string, JsonElement>
			{
				["status"] = JsonSerializer.SerializeToElement(new { value = "active" })
			})
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("Active Doc");
	}

	[SkipIfNoCluster]
	public void SearchWithBoolQuery()
	{
		var index = UniqueIndex("querydsl");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Alice", Age = 30, Category = "engineering" }, Id = "1" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Bob", Age = 25, Category = "marketing" }, Id = "2" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Charlie", Age = 35, Category = "engineering" }, Id = "3" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Diana", Age = 28, Category = "engineering" }, Id = "4" },
			]
		});

		// Bool query: must match engineering AND age >= 30
		var searchResponse = Client.Core.Search<QueryDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
			Query = QueryContainer.Bool(new BoolQuery
			{
				Must = JsonSerializer.SerializeToElement(new object[]
				{
					new { match = new { category = "engineering" } },
					new { range = new { age = new { gte = 30 } } }
				})
			})
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(2);
		searchResponse.Hits.Hits!.Select(h => h.Source!.Name)
			.Should().BeEquivalentTo(["Alice", "Charlie"]);
	}

	[SkipIfNoCluster]
	public void SearchWithExistsQuery()
	{
		var index = UniqueIndex("querydsl");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "HasCategory", Category = "engineering" }, Id = "1" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "NoCategory" }, Id = "2" },
			]
		});

		// Exists query: find docs where "category" field exists
		var searchResponse = Client.Core.Search<QueryDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
			Query = QueryContainer.Exists(new ExistsQuery { Field = "category" })
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("HasCategory");
	}

	[SkipIfNoCluster]
	public void SearchWithMatchAllQuery()
	{
		var index = UniqueIndex("querydsl");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "One" }, Id = "1" },
				new BulkIndexOperation<QueryDoc> { Document = new QueryDoc { Name = "Two" }, Id = "2" },
			]
		});

		var searchResponse = Client.Core.Search<QueryDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
			Query = QueryContainer.MatchAll(new MatchAllQuery())
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(2);
	}

	private sealed class QueryDoc
	{
		public string? Name { get; set; }
		public int Age { get; set; }
		public string? Category { get; set; }
		public string? Status { get; set; }
	}
}
