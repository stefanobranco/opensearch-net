using FluentAssertions;
using OpenSearch.Client.Cluster;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Cluster;

public class HealthTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void ClusterHealthReturnsGreenOrYellow()
	{
		var response = Client.Cluster.Health(new HealthClusterRequest());

		response.ClusterName.Should().NotBeNullOrEmpty();
		response.NumberOfNodes.Should().BeGreaterThanOrEqualTo(1);
		response.Status.Should().NotBeNull();
	}
}
