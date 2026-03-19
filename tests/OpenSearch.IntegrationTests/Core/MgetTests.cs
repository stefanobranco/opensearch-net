using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class MgetTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void MgetRetrievesMultipleDocuments()
	{
		var index = UniqueIndex("mget");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<MgetDoc> { Document = new MgetDoc { Name = "First" }, Id = "1" },
				new BulkIndexOperation<MgetDoc> { Document = new MgetDoc { Name = "Second" }, Id = "2" },
				new BulkIndexOperation<MgetDoc> { Document = new MgetDoc { Name = "Third" }, Id = "3" },
			]
		});

		// Mget using the docs array with individual IDs
		var mgetResponse = Client.Core.Mget(new MgetRequest
		{
			Index = index,
			Docs =
			[
				new Operation { Id = "1" },
				new Operation { Id = "3" },
			]
		});

		mgetResponse.Docs.Should().NotBeNull();
		mgetResponse.Docs.Should().HaveCount(2);

		// Each doc is a typed MgetResponseItem
		mgetResponse.Docs![0].Found.Should().BeTrue();
		mgetResponse.Docs[0].Id.Should().Be("1");

		mgetResponse.Docs[1].Found.Should().BeTrue();
		mgetResponse.Docs[1].Id.Should().Be("3");
	}

	[SkipIfNoCluster]
	public void MgetWithMissingDocumentReturnsFoundFalse()
	{
		var index = UniqueIndex("mget");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<MgetDoc> { Document = new MgetDoc { Name = "Existing" }, Id = "1" },
			]
		});

		var mgetResponse = Client.Core.Mget(new MgetRequest
		{
			Index = index,
			Docs =
			[
				new Operation { Id = "1" },
				new Operation { Id = "does-not-exist" },
			]
		});

		mgetResponse.Docs.Should().NotBeNull();
		mgetResponse.Docs.Should().HaveCount(2);

		// First doc found
		mgetResponse.Docs![0].Found.Should().BeTrue();

		// Second doc not found
		mgetResponse.Docs[1].Found.Should().BeFalse();
	}

	private sealed class MgetDoc
	{
		public string? Name { get; set; }
	}
}
