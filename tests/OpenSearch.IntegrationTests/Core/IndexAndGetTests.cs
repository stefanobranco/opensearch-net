using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class IndexAndGetTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void BulkIndexThenGetRoundTrip()
	{
		var index = UniqueIndex();

		// Create index first
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// Index a document via bulk
		var doc = new TestDocument { Title = "Hello OpenSearch", Count = 42 };
		var bulkResponse = Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<TestDocument>
				{
					Document = doc,
					Id = "doc-1"
				}
			]
		});

		bulkResponse.Errors.Should().BeFalse();
		bulkResponse.Items.Should().HaveCount(1);

		// Get the document back
		var getResponse = Client.Core.Get<TestDocument>(new OpenSearch.Client.Core.GetRequest
		{
			Index = index,
			Id = "doc-1"
		});

		getResponse.Found.Should().BeTrue();
		getResponse.Id.Should().Be("doc-1");
		getResponse.Source.Should().NotBeNull();
		getResponse.Source!.Title.Should().Be("Hello OpenSearch");
		getResponse.Source!.Count.Should().Be(42);
	}

	private sealed class TestDocument
	{
		public string? Title { get; set; }
		public int Count { get; set; }
	}
}
