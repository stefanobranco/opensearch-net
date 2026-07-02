using OpenSearch.Client;
using OpenSearch.Net;

namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for tests against the security-enabled cluster. The client connects over HTTPS
/// with basic auth and skips certificate validation (the cluster runs the self-signed demo
/// certificates), so every test on it exercises the TLS + authentication transport paths.
/// </summary>
public abstract class SecuredIntegrationTestBase : IDisposable
{
	private readonly List<string> _indicesToCleanup = [];

	protected OpenSearchClient Client { get; }

	protected SecuredIntegrationTestBase()
	{
		Client = new OpenSearchClient(CreateSettings());
	}

	/// <summary>Settings for the secured cluster with the admin credentials.</summary>
	protected static OpenSearchClientSettings CreateSettings(string? username = null, string? password = null) =>
		OpenSearchClientSettings
			.Create(SecuredOpenSearchCluster.Url)
			.Authentication(new BasicAuthCredentials(
				username ?? SecuredOpenSearchCluster.Username,
				password ?? SecuredOpenSearchCluster.Password))
			.SkipCertificateValidation()
			.Build();

	/// <summary>
	/// Returns a unique index name for this test run and registers it for cleanup.
	/// </summary>
	protected string UniqueIndex(string prefix = "sectest")
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
				Client.Indices.Delete(new DeleteIndexRequest { Index = [index] });
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
