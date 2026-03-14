using FluentAssertions;
using OpenSearch.Client.Cluster;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Cluster;

public class SettingsTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void GetClusterSettings()
	{
		var response = Client.Cluster.GetSettings(new GetSettingsRequest());

		// Persistent and transient should not be null (they may be empty dicts)
		response.Persistent.Should().NotBeNull();
		response.Transient.Should().NotBeNull();
	}

	[SkipIfNoCluster]
	public void PutAndRevertTransientClusterSetting()
	{
		// Set a transient cluster setting
		var putResponse = Client.Cluster.PutSettings(new PutSettingsRequest
		{
			Transient = new Dictionary<string, object>
			{
				["cluster.routing.allocation.enable"] = "all"
			}
		});

		putResponse.Acknowledged.Should().BeTrue();
		putResponse.Transient.Should().NotBeNull();

		// Verify the setting via GetSettings
		var getResponse = Client.Cluster.GetSettings(new GetSettingsRequest { FlatSettings = true });
		getResponse.Transient.Should().NotBeNull();

		// Revert: set to null to clear
		var revertResponse = Client.Cluster.PutSettings(new PutSettingsRequest
		{
			Transient = new Dictionary<string, object>
			{
				["cluster.routing.allocation.enable"] = null!
			}
		});

		revertResponse.Acknowledged.Should().BeTrue();
	}
}
