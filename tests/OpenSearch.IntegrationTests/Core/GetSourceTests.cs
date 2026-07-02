using FluentAssertions;
using OpenSearch.IntegrationTests.Infrastructure;
using OpenSearch.Client;

namespace OpenSearch.IntegrationTests.Core;

public class GetSourceTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void GetSourceReturnsTypedDocument()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new CreateIndexRequest { Index = index });
		Client.Core.Index(new IndexRequest
		{
			Index = index,
			Id = "doc-1",
			Refresh = "true",
			Body = new SourceDoc { Title = "source only", Count = 7 },
		});

		var response = Client.Core.GetSource<SourceDoc>(new GetSourceRequest { Index = index, Id = "doc-1" });

		response.Body.Should().NotBeNull();
		response.Body!.Title.Should().Be("source only");
		response.Body.Count.Should().Be(7);
	}

	private sealed class SourceDoc
	{
		public string? Title { get; set; }
		public int? Count { get; set; }
	}
}
