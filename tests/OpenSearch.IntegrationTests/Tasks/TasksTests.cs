using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Tasks;

public class TasksTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void ListReturnsRunningTasksGroupedByNode()
	{
		// _tasks always reports at least the list call itself, grouped by node.
		var response = Client.Tasks.List(new ListTaskRequest());

		response.Nodes.Should().NotBeNull();
		response.Nodes!.Should().NotBeEmpty();
	}
}
