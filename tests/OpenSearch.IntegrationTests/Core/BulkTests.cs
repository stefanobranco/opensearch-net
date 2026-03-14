using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class BulkTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void BulkIndexTenDocumentsAndVerify()
	{
		var index = UniqueIndex();

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// Bulk index 10 documents
		var operations = Enumerable.Range(1, 10).Select(i =>
			(BulkOperation)new BulkIndexOperation<BulkDoc>
			{
				Document = new BulkDoc { Title = $"Document {i}", Value = i },
				Id = $"doc-{i}"
			}).ToList();

		var bulkResponse = Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations = operations
		});

		bulkResponse.Errors.Should().BeFalse();
		bulkResponse.Items.Should().HaveCount(10);

		// Verify all documents are searchable
		var searchResponse = Client.Core.Search<BulkDoc>(new SearchRequest
		{
			Index = index,
			Size = 20,
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(10);
	}

	[SkipIfNoCluster]
	public void BulkMixedOperations()
	{
		var index = UniqueIndex();

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// First bulk: create two documents
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<BulkDoc> { Document = new BulkDoc { Title = "First", Value = 1 }, Id = "1" },
				new BulkIndexOperation<BulkDoc> { Document = new BulkDoc { Title = "Second", Value = 2 }, Id = "2" },
			]
		});

		// Second bulk: update one, delete one
		var bulkResponse = Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkUpdateOperation<BulkDoc> { Id = "1", Doc = new BulkDoc { Title = "First Updated", Value = 10 } },
				new BulkDeleteOperation { Id = "2" },
			]
		});

		bulkResponse.Errors.Should().BeFalse();

		// Verify: doc 1 updated, doc 2 deleted
		var getDoc1 = Client.Core.Get<BulkDoc>(new OpenSearch.Client.Core.GetRequest { Index = index, Id = "1" });
		getDoc1.Found.Should().BeTrue();
		getDoc1.Source!.Title.Should().Be("First Updated");

		var getDoc2 = Client.Core.Get<BulkDoc>(new OpenSearch.Client.Core.GetRequest { Index = index, Id = "2" });
		getDoc2.Found.Should().BeFalse();
	}

	private sealed class BulkDoc
	{
		public string? Title { get; set; }
		public int Value { get; set; }
	}
}
