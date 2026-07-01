using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response-deserialization fixtures for the snapshot namespace — proves the client parses a real
/// <c>_snapshot/{repo}/{snapshot}</c> payload into the typed <see cref="GetSnapshotResponse"/> /
/// <see cref="SnapshotInfo"/> shape.
/// </summary>
public class SnapshotResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Get_snapshot_response_deserializes_snapshot_info()
	{
		const string json = """
		{
		  "snapshots": [
		    {
		      "snapshot": "snapshot-1",
		      "uuid": "dKb54xw67gvdRctLCxSVeg",
		      "version_id": 136217827,
		      "version": "3.0.0",
		      "indices": ["logs-2024-01", "logs-2024-02"],
		      "data_streams": [],
		      "include_global_state": true,
		      "state": "SUCCESS",
		      "start_time": "2024-01-01T00:00:00.000Z",
		      "start_time_in_millis": 1704067200000,
		      "end_time": "2024-01-01T00:01:00.000Z",
		      "end_time_in_millis": 1704067260000,
		      "duration_in_millis": 60000,
		      "failures": [],
		      "shards": { "total": 10, "failed": 0, "successful": 10 }
		    }
		  ]
		}
		""";

		var response = Deserialize<GetSnapshotResponse>(json);

		response!.Snapshots.Should().HaveCount(1);
		var snap = response.Snapshots![0];
		snap.Snapshot.Should().Be("snapshot-1");
		snap.State.Should().Be("SUCCESS");
		snap.Indices.Should().BeEquivalentTo("logs-2024-01", "logs-2024-02");
		snap.IncludeGlobalState.Should().BeTrue();
		snap.DurationInMillis.Should().Be(60000);
		snap.Shards!.Total.Should().Be(10);
		snap.Shards.Successful.Should().Be(10);
	}

	[Fact]
	public void Get_snapshot_response_captures_partial_failure()
	{
		const string json = """
		{
		  "snapshots": [
		    {
		      "snapshot": "snapshot-2",
		      "indices": ["logs-2024-03"],
		      "state": "PARTIAL",
		      "reason": "some shards failed",
		      "shards": { "total": 5, "failed": 2, "successful": 3 }
		    }
		  ]
		}
		""";

		var response = Deserialize<GetSnapshotResponse>(json);

		var snap = response!.Snapshots![0];
		snap.State.Should().Be("PARTIAL");
		snap.Reason.Should().Be("some shards failed");
		snap.Shards!.Failed.Should().Be(2);
	}
}
