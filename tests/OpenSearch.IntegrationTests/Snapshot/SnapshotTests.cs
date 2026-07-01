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

			// The create response shape (snapshot vs accepted) varies across versions; GET below is the
			// version-robust way to inspect the finished snapshot.
			Client.Snapshot.Create(new CreateSnapshotRequest
			{
				Repository = repo,
				Snapshot = snapshot,
				Indices = [index],
				WaitForCompletion = true,
			});

			var get = Client.Snapshot.Get(new GetSnapshotRequest { Repository = repo, Snapshot = [snapshot] });
			var info = get.Snapshots.Should().ContainSingle(s => s.Snapshot == snapshot).Subject;
			info.State.Should().Be("SUCCESS");
			info.Indices.Should().Contain(index);
		}
		finally
		{
			try { Client.Snapshot.Delete(new DeleteSnapshotRequest { Repository = repo, Snapshot = snapshot }); } catch { }
			try { Client.Snapshot.DeleteRepository(new DeleteRepositorySnapshotRequest { Repository = [repo] }); } catch { }
		}
	}
}
