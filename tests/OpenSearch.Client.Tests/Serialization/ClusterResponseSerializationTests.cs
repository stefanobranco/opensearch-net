using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response-deserialization fixtures for the cluster namespace — proves the client parses real
/// <c>_cluster/health</c> and <c>_cluster/settings</c> payloads into their typed responses.
/// </summary>
public class ClusterResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Cluster_health_response_deserializes()
	{
		const string json = """
		{
		  "cluster_name": "opensearch-cluster",
		  "status": "green",
		  "timed_out": false,
		  "number_of_nodes": 3,
		  "number_of_data_nodes": 3,
		  "discovered_master": true,
		  "discovered_cluster_manager": true,
		  "active_primary_shards": 5,
		  "active_shards": 10,
		  "relocating_shards": 0,
		  "initializing_shards": 0,
		  "unassigned_shards": 0,
		  "delayed_unassigned_shards": 0,
		  "number_of_pending_tasks": 0,
		  "number_of_in_flight_fetch": 0,
		  "task_max_waiting_in_queue_millis": 0,
		  "active_shards_percent_as_number": 100.0
		}
		""";

		var response = Deserialize<HealthClusterResponse>(json);

		response!.ClusterName.Should().Be("opensearch-cluster");
		response.Status.Should().Be("green");
		response.NumberOfNodes.Should().Be(3);
		response.ActivePrimaryShards.Should().Be(5);
		response.ActiveShards.Should().Be(10);
		response.DiscoveredClusterManager.Should().BeTrue();
		response.ActiveShardsPercentAsNumber.Should().Be(100.0);
	}

	[Fact]
	public void Cluster_settings_response_deserializes_persistent_and_transient()
	{
		const string json = """
		{
		  "persistent": { "cluster": { "max_shards_per_node": "2000" } },
		  "transient": { "cluster": { "routing": { "allocation": { "enable": "all" } } } },
		  "defaults": {}
		}
		""";

		var response = Deserialize<GetSettingsClusterResponse>(json);

		response!.Persistent.Should().ContainKey("cluster");
		response.Transient.Should().ContainKey("cluster");
	}
}
