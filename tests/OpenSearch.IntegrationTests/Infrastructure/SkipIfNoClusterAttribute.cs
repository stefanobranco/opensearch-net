using Xunit;

namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Skips the test if no OpenSearch cluster is available.
/// </summary>
public sealed class SkipIfNoClusterAttribute : FactAttribute
{
	public SkipIfNoClusterAttribute()
	{
		if (!OpenSearchCluster.IsAvailable())
			Skip = "OpenSearch cluster is not available. Start one with: docker compose up -d";
	}
}
