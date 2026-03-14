using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides a configured OpenSearchClient,
/// unique index names, and cleanup on dispose.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
	private readonly List<string> _indicesToCleanup = [];

	protected OpenSearchClient Client { get; }

	protected IntegrationTestBase()
	{
		Client = new OpenSearchClient(OpenSearchCluster.Url);
	}

	/// <summary>
	/// Returns a unique index name for this test run and registers it for cleanup.
	/// </summary>
	protected string UniqueIndex(string prefix = "test")
	{
		var name = $"{prefix}-{Guid.NewGuid():N}";
		_indicesToCleanup.Add(name);
		return name;
	}

	public void Dispose()
	{
		foreach (var index in _indicesToCleanup)
		{
			try
			{
				Client.Indices.Delete(new OpenSearch.Client.Indices.DeleteIndexRequest { Index = [index] });
			}
			catch
			{
				// Best effort cleanup
			}
		}
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
