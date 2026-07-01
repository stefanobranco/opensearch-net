using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Snapshot;

public class SnapshotTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void RepositoryAndSnapshotLifecycle()
	{
		var repo = $"test-repo-{Guid.NewGuid():N}";
		var snapshot = $"snap-{Guid.NewGuid():N}";
		var index = UniqueIndex("snap-src");

		try
		{
			// Register an fs repository (CI sets path.repo=/tmp/snapshots).
			var createRepo = Client.Snapshot.CreateRepository(new CreateRepositorySnapshotRequest
			{
				Repository = repo,
				Type = "fs",
				Settings = new RepositorySettings { Location = "/tmp/snapshots" },
			});
			createRepo.Acknowledged.Should().BeTrue();

			// Something to snapshot.
			Client.Indices.Create(new CreateIndexRequest { Index = index });

			var create = Client.Snapshot.Create(new CreateSnapshotRequest
			{
				Repository = repo,
				Snapshot = snapshot,
				Indices = [index],
				WaitForCompletion = true,
			});
			create.Snapshot!.State.Should().Be("SUCCESS");
			create.Snapshot.Indices.Should().Contain(index);

			var get = Client.Snapshot.Get(new GetSnapshotRequest { Repository = repo, Snapshot = [snapshot] });
			get.Snapshots.Should().Contain(s => s.Snapshot == snapshot);

			Client.Snapshot.Delete(new DeleteSnapshotRequest { Repository = repo, Snapshot = snapshot })
				.Acknowledged.Should().BeTrue();
		}
		finally
		{
			try { Client.Snapshot.DeleteRepository(new DeleteRepositorySnapshotRequest { Repository = [repo] }); } catch { }
		}
	}
}
