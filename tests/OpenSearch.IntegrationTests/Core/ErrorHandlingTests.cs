using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.Net;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class ErrorHandlingTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void SearchOnNonExistentIndex_ReturnsInvalidWithServerError()
	{
		// Search on a non-existent index returns a response with IsValid=false
		// and ServerError populated (non-throwing by default)
		var response = Client.Core.Search<ErrorDoc>(new SearchRequest
		{
			Index = ["this-index-does-not-exist-" + Guid.NewGuid().ToString("N")]
		});

		response.IsValid.Should().BeFalse();
		response.ServerError.Should().NotBeNull();
		response.ServerError!.Error!.Type.Should().Be("index_not_found_exception");
	}

	[SkipIfNoCluster]
	public void GetNonExistentDocument_ReturnsFoundFalse()
	{
		var index = UniqueIndex("err");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		// GET on non-existent doc returns found=false (not an exception)
		var getResponse = Client.Core.Get<ErrorDoc>(new GetRequest { Index = index, Id = "does-not-exist" });
		getResponse.Found.Should().BeFalse();
	}

	[SkipIfNoCluster]
	public void ExistsOnNonExistentDocument_ReturnsFalse()
	{
		var index = UniqueIndex("err");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		var existsResponse = Client.Core.Exists(new ExistsRequest { Index = index, Id = "does-not-exist" });
		existsResponse.Exists.Should().BeFalse();
	}

	[SkipIfNoCluster]
	public void ExistsOnExistingDocument_ReturnsTrue()
	{
		var index = UniqueIndex("err");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<ErrorDoc> { Document = new ErrorDoc { Name = "exists-test" }, Id = "1" }
			]
		});

		var existsResponse = Client.Core.Exists(new ExistsRequest { Index = index, Id = "1" });
		existsResponse.Exists.Should().BeTrue();
	}

	private sealed class ErrorDoc
	{
		public string? Name { get; set; }
	}
}
