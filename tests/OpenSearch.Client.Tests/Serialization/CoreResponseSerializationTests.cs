using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response fixtures for _core operations whose response is a <c>oneOf</c> of the full result and an
/// async <c>{ "task": ... }</c> reference (delete_by_query / update_by_query / reindex). The generator
/// merges the union into a typed superset; these prove both wire forms parse. Previously the entire
/// response was dropped (an empty type).
/// </summary>
public class CoreResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Delete_by_query_response_deserializes_full_result()
	{
		const string json = """
		{
		  "took": 147,
		  "timed_out": false,
		  "total": 5,
		  "deleted": 5,
		  "batches": 1,
		  "version_conflicts": 0,
		  "noops": 0,
		  "retries": { "bulk": 0, "search": 0 },
		  "throttled_millis": 0,
		  "requests_per_second": -1.0,
		  "throttled_until_millis": 0,
		  "failures": []
		}
		""";

		var response = Deserialize<DeleteByQueryResponse>(json);

		response!.Took.Should().Be(147);
		response.TimedOut.Should().BeFalse();
		response.Total.Should().Be(5);
		response.Deleted.Should().Be(5);
		response.Batches.Should().Be(1);
		response.Task.Should().BeNull("the synchronous result form has no task reference");
	}

	[Fact]
	public void Delete_by_query_response_deserializes_async_task_form()
	{
		// wait_for_completion=false returns just the task id — the other arm of the same oneOf.
		var response = Deserialize<DeleteByQueryResponse>("""{"task":"oTUltX4IQMOUUVeiohTt8A:123"}""");

		response!.Task.Should().Be("oTUltX4IQMOUUVeiohTt8A:123");
		response.Deleted.Should().BeNull();
	}

	[Fact]
	public void Reindex_response_deserializes_created_count()
	{
		const string json = """
		{ "took": 42, "timed_out": false, "total": 3, "created": 3, "updated": 0, "batches": 1, "failures": [] }
		""";

		var response = Deserialize<ReindexResponse>(json);

		response!.Total.Should().Be(3);
		response.Created.Should().Be(3);
	}
}
