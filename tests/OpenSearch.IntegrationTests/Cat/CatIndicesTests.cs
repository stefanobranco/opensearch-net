using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Cat;

public class CatIndicesTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void CatIndicesReturnsCreatedIndex()
	{
		var index = UniqueIndex("catidx");

		Client.Indices.Create(new OpenSearch.Client.CreateIndexRequest { Index = index });

		// Index a document so the index is not empty
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<CatDoc> { Document = new CatDoc { Name = "test" }, Id = "1" }
			]
		});

		// Cat indices should not throw - the response type is empty but the request succeeds
		var act = () => Client.Cat.Indices(new IndicesCatRequest { Index = [index] });
		act.Should().NotThrow();
	}

	[SkipIfNoCluster]
	public void CatIndicesAllIndices()
	{
		// Calling cat indices without a specific index should not throw
		var act = () => Client.Cat.Indices(new IndicesCatRequest());
		act.Should().NotThrow();
	}

	private sealed class CatDoc
	{
		public string? Name { get; set; }
	}
}
