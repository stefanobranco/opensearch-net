using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Nodes;

public class NodesTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void InfoReturnsClusterNameAndNodeSummary()
	{
		var response = Client.Nodes.Info(new InfoNodeRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
		response.Nodes!.Total.Should().BeGreaterThanOrEqualTo(1);
	}

	[SkipIfNoCluster]
	public void StatsReturnsNodeSummary()
	{
		var response = Client.Nodes.Stats(new StatsNodeRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
		response.Nodes!.Total.Should().BeGreaterThanOrEqualTo(1);
	}

	[SkipIfNoCluster]
	public void HotThreadsReturnsPlainTextDump()
	{
		// nodes.hot_threads is a text/plain endpoint — the raw dump is captured in Value.
		var response = Client.Nodes.HotThreads(new HotThreadsNodeRequest());

		response.Value.Should().NotBeNullOrEmpty();
	}
}
