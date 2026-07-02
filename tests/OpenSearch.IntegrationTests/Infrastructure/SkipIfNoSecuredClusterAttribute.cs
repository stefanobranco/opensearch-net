using Xunit;

namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Skips the test if no security-enabled OpenSearch cluster is available.
/// </summary>
public sealed class SkipIfNoSecuredClusterAttribute : FactAttribute
{
	public SkipIfNoSecuredClusterAttribute()
	{
		if (!SecuredOpenSearchCluster.IsAvailable())
			Skip = "Secured OpenSearch cluster is not available. Start one with: docker compose --profile secured up -d";
	}
}
