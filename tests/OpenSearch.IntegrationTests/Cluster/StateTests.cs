using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Cluster;

public class StateTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void ClusterStateCapturesFreeformBody()
	{
		var response = Client.Cluster.State(new StateClusterRequest());

		response.ExtensionData.Should().NotBeNullOrEmpty();
		response.ExtensionData!.Should().ContainKey("cluster_name");
		response.ExtensionData.Should().ContainKey("nodes");
	}
}
