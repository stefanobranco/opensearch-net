using FluentAssertions;
using OpenSearch.Client.Indices;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Indices;

public class IndexLifecycleTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void CreateExistsDeleteLifecycle()
	{
		var index = UniqueIndex();

		// Create
		var createResponse = Client.Indices.Create(new CreateIndexRequest { Index = index });
		createResponse.Acknowledged.Should().BeTrue();

		// Exists
		var existsResponse = Client.Indices.Exists(new ExistsIndexRequest { Index = [index] });
		existsResponse.Exists.Should().BeTrue();

		// Delete
		var deleteResponse = Client.Indices.Delete(new DeleteIndexRequest { Index = [index] });
		deleteResponse.Acknowledged.Should().BeTrue();

		// Verify deleted
		var existsAfterDelete = Client.Indices.Exists(new ExistsIndexRequest { Index = [index] });
		existsAfterDelete.Exists.Should().BeFalse();
	}
}
