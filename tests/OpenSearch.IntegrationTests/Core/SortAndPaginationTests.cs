using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class SortAndPaginationTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void SearchWithFromAndSize()
	{
		var index = UniqueIndex("paging");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		// Index 5 documents
		var operations = Enumerable.Range(1, 5).Select(i =>
			(BulkOperation)new BulkIndexOperation<PageDoc>
			{
				Document = new PageDoc { Name = $"Doc-{i}", Score = i },
				Id = $"{i}"
			}).ToList();

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations = operations
		});

		// Page 1: from=0, size=2
		var page1 = Client.Core.Search<PageDoc>(new SearchRequest
		{
			Index = [index],
			From = 0,
			Size = 2,
			Sort = JsonSerializer.SerializeToElement("score:asc")
		});

		page1.Hits.Should().NotBeNull();
		page1.Hits!.Hits.Should().HaveCount(2);
		page1.Hits.Hits![0].Source!.Score.Should().Be(1);
		page1.Hits.Hits[1].Source!.Score.Should().Be(2);

		// Page 2: from=2, size=2
		var page2 = Client.Core.Search<PageDoc>(new SearchRequest
		{
			Index = [index],
			From = 2,
			Size = 2,
			Sort = JsonSerializer.SerializeToElement("score:asc")
		});

		page2.Hits.Should().NotBeNull();
		page2.Hits!.Hits.Should().HaveCount(2);
		page2.Hits.Hits![0].Source!.Score.Should().Be(3);
		page2.Hits.Hits[1].Source!.Score.Should().Be(4);

		// Page 3: from=4, size=2 (only 1 remaining)
		var page3 = Client.Core.Search<PageDoc>(new SearchRequest
		{
			Index = [index],
			From = 4,
			Size = 2,
			Sort = JsonSerializer.SerializeToElement("score:asc")
		});

		page3.Hits.Should().NotBeNull();
		page3.Hits!.Hits.Should().HaveCount(1);
		page3.Hits.Hits![0].Source!.Score.Should().Be(5);
	}

	[SkipIfNoCluster]
	public void SearchWithSortDescending()
	{
		var index = UniqueIndex("sort");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "Low", Score = 10 }, Id = "1" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "High", Score = 100 }, Id = "2" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "Mid", Score = 50 }, Id = "3" },
			]
		});

		var searchResponse = Client.Core.Search<PageDoc>(new SearchRequest
		{
			Index = [index],
			Size = 10,
			Sort = JsonSerializer.SerializeToElement("score:desc")
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(3);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("High");
		searchResponse.Hits.Hits[1].Source!.Name.Should().Be("Mid");
		searchResponse.Hits.Hits[2].Source!.Name.Should().Be("Low");
	}

	[SkipIfNoCluster]
	public void CountDocumentsInIndex()
	{
		var index = UniqueIndex("count");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "A" }, Id = "1" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "B" }, Id = "2" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "C" }, Id = "3" },
			]
		});

		var countResponse = Client.Core.Count(new CountRequest { Index = [index] });
		countResponse.Count.Should().Be(3);
	}

	[SkipIfNoCluster]
	public void CountWithQuery()
	{
		var index = UniqueIndex("count");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "Alice", Score = 10 }, Id = "1" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "Bob", Score = 20 }, Id = "2" },
				new BulkIndexOperation<PageDoc> { Document = new PageDoc { Name = "Charlie", Score = 30 }, Id = "3" },
			]
		});

		var countResponse = Client.Core.Count(new CountRequest
		{
			Index = [index],
			Query = QueryContainer.Term("name.keyword", new OpenSearch.Client.Core.TermQuery { Value = JsonSerializer.SerializeToElement("Alice") })
		});

		countResponse.Count.Should().Be(1);
	}

	private sealed class PageDoc
	{
		public string? Name { get; set; }
		public int Score { get; set; }
	}
}
