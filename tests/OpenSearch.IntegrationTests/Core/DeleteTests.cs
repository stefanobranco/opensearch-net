using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class DeleteTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void DeleteExistingDocument()
	{
		var index = UniqueIndex("delete");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<DeleteDoc> { Document = new DeleteDoc { Name = "to-be-deleted" }, Id = "1" }
			]
		});

		// Verify the document exists
		var getResponse = Client.Core.Get<DeleteDoc>(new GetRequest { Index = index, Id = "1" });
		getResponse.Found.Should().BeTrue();

		// Delete it
		var deleteResponse = Client.Core.Delete(new DeleteRequest { Index = index, Id = "1" });
		deleteResponse.Result.Should().NotBeNull();
		deleteResponse.Result!.Value.GetString().Should().Be("deleted");

		// Verify it's gone
		var getAfterDelete = Client.Core.Get<DeleteDoc>(new GetRequest { Index = index, Id = "1" });
		getAfterDelete.Found.Should().BeFalse();
	}

	[SkipIfNoCluster]
	public void DeleteNonExistentDocument_ReturnsNotFound()
	{
		var index = UniqueIndex("delete");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// DELETE on non-existent doc returns result "not_found" (404 is not thrown for DELETE)
		var response = Client.Core.Delete(new DeleteRequest { Index = index, Id = "nonexistent" });
		response.Result.Should().NotBeNull();
		response.Result!.Value.GetString().Should().Be("not_found");
	}

	[SkipIfNoCluster]
	public void DeleteByQuery_RemovesMatchingDocuments()
	{
		var index = UniqueIndex("dbq");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<DeleteDoc> { Document = new DeleteDoc { Name = "keep" }, Id = "1" },
				new BulkIndexOperation<DeleteDoc> { Document = new DeleteDoc { Name = "remove" }, Id = "2" },
				new BulkIndexOperation<DeleteDoc> { Document = new DeleteDoc { Name = "remove" }, Id = "3" },
			]
		});

		// Delete by query using the Q parameter
		Client.Core.DeleteByQuery(new DeleteByQueryRequest
		{
			Index = index,
			Q = "name:remove",
			Refresh = System.Text.Json.JsonSerializer.SerializeToElement("true")
		});

		// Verify only the "keep" doc remains
		var searchResponse = Client.Core.Search<DeleteDoc>(new SearchRequest { Index = index, Size = 10 });
		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("keep");
	}

	private sealed class DeleteDoc
	{
		public string? Name { get; set; }
	}
}
