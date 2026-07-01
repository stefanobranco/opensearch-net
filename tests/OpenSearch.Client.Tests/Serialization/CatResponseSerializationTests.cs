using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response fixtures for the cat APIs. With <c>format=json</c> (injected by the generated request) a
/// cat endpoint returns a JSON array of records; the response derives from <c>List&lt;Record&gt;</c>.
/// Previously the entire body was discarded (empty type + <c>new()</c> deserialization), and the
/// records' dotted columns (<c>docs.count</c>, <c>store.size</c>) were dropped.
/// </summary>
public class CatResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Cat_indices_response_deserializes_row_list_with_dotted_columns()
	{
		const string json = """
		[
		  {
		    "health": "green", "status": "open", "index": "logs-2024-01", "uuid": "u1",
		    "pri": "1", "rep": "1", "docs.count": "1000", "docs.deleted": "5", "store.size": "5.2mb"
		  },
		  {
		    "health": "yellow", "status": "open", "index": "logs-2024-02", "uuid": "u2",
		    "docs.count": "2000", "store.size": "9.8mb"
		  }
		]
		""";

		var response = Deserialize<IndicesCatResponse>(json);

		response.Should().HaveCount(2);

		var first = response![0];
		first.Index.Should().Be("logs-2024-01");
		first.Health.Should().Be("green");
		first.Status.Should().Be("open");
		first.DocsCount.Should().Be("1000");   // dotted column recovered
		first.DocsDeleted.Should().Be("5");    // dotted column recovered
		first.StoreSize.Should().Be("5.2mb");  // dotted column recovered

		response[1].Index.Should().Be("logs-2024-02");
		response[1].DocsCount.Should().Be("2000");
	}

	[Fact]
	public void Cat_indices_empty_response_deserializes_to_empty_list()
	{
		var response = Deserialize<IndicesCatResponse>("[]");
		response.Should().NotBeNull();
		response.Should().BeEmpty();
	}
}
