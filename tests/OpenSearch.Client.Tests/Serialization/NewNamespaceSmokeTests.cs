using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Smoke fixtures for the newly wired namespaces: a JSON response round-trip (tasks) and the
/// <c>text/plain</c> response path (ubi, nodes.hot_threads) — responses the generator previously
/// left empty and tried to JSON-deserialize (a runtime failure on a non-JSON body).
/// </summary>
public class NewNamespaceSmokeTests : SerializationTestBase
{
	[Fact]
	public void Get_task_response_deserializes_task_info()
	{
		const string json = """
		{
		  "completed": true,
		  "task": {
		    "node": "node-1",
		    "id": 42,
		    "type": "transport",
		    "action": "indices:data/write/reindex",
		    "description": "reindex from a to b"
		  }
		}
		""";

		var response = Deserialize<GetTaskResponse>(json);

		response!.Completed.Should().BeTrue();
		response.Task.Should().NotBeNull();
		response.Task!.Action.Should().Be("indices:data/write/reindex");
	}

	[Fact]
	public void Ubi_initialize_reads_plain_text_response_body()
	{
		// ubi.initialize returns text/plain — the endpoint must read the raw body, not JSON-parse it.
		const string body = "UBI indices initialized";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));

		var response = InitializeUbiEndpoint.Instance.DeserializeResponse(200, "text/plain", stream, Serializer);

		response.Value.Should().Be("UBI indices initialized");
	}

	[Fact]
	public void Hot_threads_reads_plain_text_thread_dump()
	{
		// nodes.hot_threads returns a plain-text thread dump — another previously-broken text/plain path.
		const string dump = "::: {node-1}\n   Hot threads at 2024-01-01, interval=500ms:\n   0.0% cpu usage";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(dump));

		var response = HotThreadsNodeEndpoint.Instance.DeserializeResponse(200, "text/plain", stream, Serializer);

		response.Value.Should().Contain("Hot threads");
	}
}
