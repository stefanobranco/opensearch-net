using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class UpdateTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void UpdateDocumentWithPartialDoc()
	{
		var index = UniqueIndex("update");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		// Index a document
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<UpdateDoc>
				{
					Document = new UpdateDoc { Name = "Original", Value = 1 },
					Id = "1"
				}
			]
		});

		// Update the document with a partial doc
		var updateResponse = Client.Core.Update<UpdateDoc>(new UpdateRequest
		{
			Index = index,
			Id = "1",
			Doc = JsonSerializer.SerializeToElement(new { value = 42 }),
			Refresh = "true"
		});

		updateResponse.Result.Should().NotBeNull();
		updateResponse.Result.Should().Be("updated");

		// Verify the update
		var getResponse = Client.Core.Get<UpdateDoc>(new GetRequest { Index = index, Id = "1" });
		getResponse.Found.Should().BeTrue();
		getResponse.Source!.Name.Should().Be("Original"); // unchanged
		getResponse.Source!.Value.Should().Be(42); // updated
	}

	[SkipIfNoCluster]
	public void UpdateDocumentWithScript()
	{
		var index = UniqueIndex("update");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<UpdateDoc>
				{
					Document = new UpdateDoc { Name = "Scripted", Value = 10 },
					Id = "1"
				}
			]
		});

		// Update using a script
		var updateResponse = Client.Core.Update<UpdateDoc>(new UpdateRequest
		{
			Index = index,
			Id = "1",
			Script = JsonSerializer.SerializeToElement(new
			{
				source = "ctx._source.value += params.increment",
				lang = "painless",
				@params = new { increment = 5 }
			}),
			Refresh = "true"
		});

		updateResponse.Result.Should().NotBeNull();
		updateResponse.Result.Should().Be("updated");

		// Verify the script update
		var getResponse = Client.Core.Get<UpdateDoc>(new GetRequest { Index = index, Id = "1" });
		getResponse.Found.Should().BeTrue();
		getResponse.Source!.Value.Should().Be(15);
	}

	[SkipIfNoCluster]
	public void UpsertCreatesDocumentIfNotExists()
	{
		var index = UniqueIndex("update");

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateIndexRequest { Index = index });

		// Upsert: doc doesn't exist, so upsert value is used
		var updateResponse = Client.Core.Update<UpdateDoc>(new UpdateRequest
		{
			Index = index,
			Id = "new-doc",
			Doc = JsonSerializer.SerializeToElement(new { name = "Updated", value = 99 }),
			Upsert = JsonSerializer.SerializeToElement(new { name = "Upserted", value = 1 }),
			Refresh = "true"
		});

		updateResponse.Result.Should().NotBeNull();
		updateResponse.Result.Should().Be("created");

		// Verify the upsert created the document
		var getResponse = Client.Core.Get<UpdateDoc>(new GetRequest { Index = index, Id = "new-doc" });
		getResponse.Found.Should().BeTrue();
		getResponse.Source!.Name.Should().Be("Upserted");
		getResponse.Source!.Value.Should().Be(1);
	}

	private sealed class UpdateDoc
	{
		public string? Name { get; set; }
		public int Value { get; set; }
	}
}
