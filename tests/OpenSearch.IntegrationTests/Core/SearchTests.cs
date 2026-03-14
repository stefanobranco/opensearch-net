using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class SearchTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void BulkIndexThenSearch()
	{
		var index = UniqueIndex();

		// Create index
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// Index documents via bulk with refresh
		var bulkResponse = Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<SearchDoc> { Document = new SearchDoc { Name = "Alice", Age = 30 }, Id = "1" },
				new BulkIndexOperation<SearchDoc> { Document = new SearchDoc { Name = "Bob", Age = 25 }, Id = "2" },
				new BulkIndexOperation<SearchDoc> { Document = new SearchDoc { Name = "Charlie", Age = 35 }, Id = "3" },
			]
		});

		bulkResponse.Errors.Should().BeFalse();

		// Search all documents
		var searchResponse = Client.Core.Search<SearchDoc>(new SearchRequest
		{
			Index = index,
			Size = 10,
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(3);
		searchResponse.Hits.Hits!.Select(h => h.Source!.Name)
			.Should().BeEquivalentTo(["Alice", "Bob", "Charlie"]);
	}

	[SkipIfNoCluster]
	public void SearchWithQueryString()
	{
		var index = UniqueIndex();

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<SearchDoc> { Document = new SearchDoc { Name = "Alice Smith", Age = 30 }, Id = "1" },
				new BulkIndexOperation<SearchDoc> { Document = new SearchDoc { Name = "Bob Jones", Age = 25 }, Id = "2" },
			]
		});

		// Search with query string
		var searchResponse = Client.Core.Search<SearchDoc>(new SearchRequest
		{
			Index = index,
			Q = "Alice",
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("Alice Smith");
	}

	private sealed class SearchDoc
	{
		public string? Name { get; set; }
		public int Age { get; set; }
	}
}
