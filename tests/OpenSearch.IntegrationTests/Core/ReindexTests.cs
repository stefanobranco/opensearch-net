using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class ReindexTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void ReindexCopiesDocumentsToNewIndex()
	{
		var sourceIndex = UniqueIndex("reindex-src");
		var destIndex = UniqueIndex("reindex-dst");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = sourceIndex });
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = destIndex });

		// Index 3 documents in the source
		Client.Core.Bulk(new BulkRequest
		{
			Index = sourceIndex,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<ReindexDoc> { Document = new ReindexDoc { Name = "Doc1" }, Id = "1" },
				new BulkIndexOperation<ReindexDoc> { Document = new ReindexDoc { Name = "Doc2" }, Id = "2" },
				new BulkIndexOperation<ReindexDoc> { Document = new ReindexDoc { Name = "Doc3" }, Id = "3" },
			]
		});

		// Reindex from source to destination
		Client.Core.Reindex(new ReindexRequest
		{
			Source = new Source { Index = sourceIndex },
			Dest = new Destination { Index = destIndex },
			Refresh = System.Text.Json.JsonSerializer.SerializeToElement(true)
		});

		// Verify all documents are in the destination index
		var searchResponse = Client.Core.Search<ReindexDoc>(new SearchRequest
		{
			Index = destIndex,
			Size = 10
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(3);
		searchResponse.Hits.Hits!.Select(h => h.Source!.Name)
			.Should().BeEquivalentTo(["Doc1", "Doc2", "Doc3"]);
	}

	private sealed class ReindexDoc
	{
		public string? Name { get; set; }
	}
}
