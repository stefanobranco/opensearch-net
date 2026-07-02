using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Nodes;

public class NodesTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void InfoReturnsPerNodeDetails()
	{
		var response = Client.Nodes.Info(new InfoNodeRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
		response.NodeStatistics!.Total.Should().BeGreaterThanOrEqualTo(1);
		response.Nodes.Should().NotBeNullOrEmpty();

		var node = response.Nodes!.Values.First();
		node.Name.Should().NotBeNullOrEmpty();
		node.Version.Should().NotBeNullOrEmpty();
		node.Roles.Should().NotBeNullOrEmpty();
		node.Os!.AvailableProcessors.Should().BeGreaterThan(0);
	}

	[SkipIfNoCluster]
	public void StatsReturnsPerNodeDetails()
	{
		var response = Client.Nodes.Stats(new StatsNodeRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
		response.NodeStatistics!.Total.Should().BeGreaterThanOrEqualTo(1);
		response.Nodes.Should().NotBeNullOrEmpty();

		var node = response.Nodes!.Values.First();
		node.Indices!.Docs!.Count.Should().BeGreaterThanOrEqualTo(0);
		node.Jvm!.Mem!.HeapUsedInBytes.Should().BeGreaterThan(0);
	}

	[SkipIfNoCluster]
	public void HotThreadsReturnsPlainTextDump()
	{
		// nodes.hot_threads is a text/plain endpoint — the raw dump is captured in Value.
		var response = Client.Nodes.HotThreads(new HotThreadsNodeRequest());

		response.Value.Should().NotBeNullOrEmpty();
	}
}
