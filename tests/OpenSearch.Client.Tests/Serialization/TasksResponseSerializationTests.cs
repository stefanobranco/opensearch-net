using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response-deserialization fixtures for the tasks namespace — proves the client parses a real
/// <c>_tasks</c> listing into the nested <see cref="ListTaskResponse"/> /
/// <see cref="TaskExecutingNode"/> / <see cref="TaskInfo"/> shape.
/// </summary>
public class TasksResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void List_tasks_response_deserializes_grouped_by_node()
	{
		const string json = """
		{
		  "nodes": {
		    "oTUltX4IQMOUUVeiohTt8A": {
		      "name": "node-1",
		      "transport_address": "127.0.0.1:9300",
		      "host": "127.0.0.1",
		      "ip": "127.0.0.1:9300",
		      "roles": ["data", "cluster_manager"],
		      "tasks": {
		        "oTUltX4IQMOUUVeiohTt8A:123": {
		          "node": "oTUltX4IQMOUUVeiohTt8A",
		          "id": 123,
		          "type": "transport",
		          "action": "indices:data/write/reindex",
		          "description": "reindex from a to b",
		          "cancellable": true,
		          "start_time_in_millis": 1704067200000,
		          "running_time_in_nanos": 5000000
		        }
		      }
		    }
		  }
		}
		""";

		var response = Deserialize<ListTaskResponse>(json);

		response!.Nodes.Should().ContainKey("oTUltX4IQMOUUVeiohTt8A");
		var node = response.Nodes!["oTUltX4IQMOUUVeiohTt8A"];
		node.Name.Should().Be("node-1");
		node.Roles.Should().Contain("cluster_manager");

		var task = node.Tasks!["oTUltX4IQMOUUVeiohTt8A:123"];
		task.Id.Should().Be(123);
		task.Action.Should().Be("indices:data/write/reindex");
		task.Cancellable.Should().BeTrue();
		task.RunningTimeInNanos.Should().Be(5000000);
	}

	[Fact]
	public void Cancel_task_response_deserializes_node_failures()
	{
		const string json = """
		{
		  "node_failures": [
		    { "type": "failed_node_exception", "reason": "Failed node [abc]" }
		  ],
		  "nodes": {}
		}
		""";

		var response = Deserialize<CancelTaskResponse>(json);

		response!.NodeFailures.Should().HaveCount(1);
		response.NodeFailures![0].Type.Should().Be("failed_node_exception");
	}
}
