using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class IndexDocumentTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void IndexDocumentViaIndexApi()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		var doc = new IndexDoc { Title = "Via Index API", Score = 99 };

		// Index using the IndexRequest.Body property (raw body)
		var indexResponse = Client.Core.Index(new IndexRequest
		{
			Index = index,
			Id = "idx-1",
			Body = doc,
			Refresh = "true"
		});

		indexResponse.Should().NotBeNull();
		indexResponse.Result.Should().NotBeNull();
		indexResponse.Result.Should().Be("created");

		// Verify via GET
		var getResponse = Client.Core.Get<IndexDoc>(new GetRequest
		{
			Index = index,
			Id = "idx-1"
		});

		getResponse.Found.Should().BeTrue();
		getResponse.Source.Should().NotBeNull();
		getResponse.Source!.Title.Should().Be("Via Index API");
		getResponse.Source!.Score.Should().Be(99);
	}

	[SkipIfNoCluster]
	public void IndexDocumentWithoutId()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		var doc = new IndexDoc { Title = "Auto ID", Score = 7 };

		var indexResponse = Client.Core.Index(new IndexRequest
		{
			Index = index,
			Body = doc,
			Refresh = "true"
		});

		indexResponse.Should().NotBeNull();
		indexResponse.Result.Should().NotBeNull();
		indexResponse.Result.Should().Be("created");
		indexResponse.Id.Should().NotBeNullOrEmpty();
	}

	[SkipIfNoCluster]
	public void CreateDocumentViaCreateApi()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		var doc = new IndexDoc { Title = "Via Create API", Score = 55 };

		var createResponse = Client.Core.Create(new CreateRequest
		{
			Index = index,
			Id = "create-1",
			Body = doc,
			Refresh = "true"
		});

		createResponse.Should().NotBeNull();
		createResponse.Result.Should().NotBeNull();
		createResponse.Result.Should().Be("created");

		// Verify via GET
		var getResponse = Client.Core.Get<IndexDoc>(new GetRequest
		{
			Index = index,
			Id = "create-1"
		});

		getResponse.Found.Should().BeTrue();
		getResponse.Source!.Title.Should().Be("Via Create API");
	}

	private sealed class IndexDoc
	{
		public string? Title { get; set; }
		public int Score { get; set; }
	}
}
