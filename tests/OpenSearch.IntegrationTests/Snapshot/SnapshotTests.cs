using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Snapshot;

public class SnapshotTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void RepositoryLifecycle()
	{
		// Repository registration is the deterministic core of the snapshot namespace. (Full
		// snapshot create/get is eventually-consistent and flaky on the resource-constrained CI
		// cluster, so it is left to the serialization fixtures.)
		var repo = $"test-repo-{Guid.NewGuid():N}";

		try
		{
			var create = Client.Snapshot.CreateRepository(new CreateRepositorySnapshotRequest
			{
				Repository = repo,
				Type = "fs",
				Settings = new RepositorySettings { Location = "/tmp/snapshots" },
			});
			create.Acknowledged.Should().BeTrue();

			var get = Client.Snapshot.GetRepository(new GetRepositorySnapshotRequest { Repository = [repo] });
			get.Should().ContainKey(repo);
			get[repo].Type.Should().Be("fs");
		}
		finally
		{
			try { Client.Snapshot.DeleteRepository(new DeleteRepositorySnapshotRequest { Repository = [repo] }); } catch { }
		}
	}
}
