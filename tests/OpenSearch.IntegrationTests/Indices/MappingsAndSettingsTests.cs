using FluentAssertions;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using OpenSearch.Client.Indices;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Indices;

public class MappingsAndSettingsTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void PutMappingAddsFieldToIndex()
	{
		var index = UniqueIndex("mapping");

		Client.Indices.Create(new CreateIndexRequest { Index = index });

		// Add a new field mapping
		var putMappingResponse = Client.Indices.PutMapping(new PutMappingIndexRequest
		{
			Index = [index],
			Properties = new Dictionary<string, Property>
			{
				["title"] = Property.TextProperty(new TextProperty()),
				["count"] = Property.IntegerNumberProperty(new IntegerNumberProperty())
			}
		});

		putMappingResponse.Acknowledged.Should().BeTrue();

		// Verify the mapping was applied
		var getMappingResponse = Client.Indices.GetMapping(new GetMappingIndexRequest { Index = [index] });
		getMappingResponse.Should().ContainKey(index);
	}

	[SkipIfNoCluster]
	public void CreateIndexWithMappingsAndSettings()
	{
		var index = UniqueIndex("mapped");

		var createResponse = Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { NumberOfShards = "1" },
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, Property>
				{
					["name"] = Property.TextProperty(new TextProperty()),
					["age"] = Property.IntegerNumberProperty(new IntegerNumberProperty()),
				["status"] = Property.KeywordProperty(new KeywordProperty())
				}
			}
		});

		createResponse.Acknowledged.Should().BeTrue();

		// Update replicas to 0 after creation
		Client.Indices.PutSettings(new PutSettingsIndexRequest
		{
			Index = [index],
			NumberOfReplicas = "0"
		});

		// Index a document conforming to the mapping
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<MappingDoc>
				{
					Document = new MappingDoc { Name = "Test", Age = 30, Status = "active" },
					Id = "1"
				}
			]
		});

		// Verify we can search on the keyword field
		var searchResponse = Client.Core.Search<MappingDoc>(new SearchRequest
		{
			Index = [index],
			Q = "status:active"
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
	}

	[SkipIfNoCluster]
	public void PutSettingsUpdatesIndexSettings()
	{
		var index = UniqueIndex("settings");

		Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { NumberOfShards = "1" }
		});

		// Update the number of replicas
		var putSettingsResponse = Client.Indices.PutSettings(new PutSettingsIndexRequest
		{
			Index = [index],
			NumberOfReplicas = "0"
		});

		putSettingsResponse.Acknowledged.Should().BeTrue();

		// Verify the setting was updated
		var getSettingsResponse = Client.Indices.GetSettings(new GetSettingsIndexRequest
		{
			Index = [index],
			FlatSettings = true
		});

		getSettingsResponse.Should().ContainKey(index);
	}

	private sealed class MappingDoc
	{
		public string? Name { get; set; }
		public int Age { get; set; }
		public string? Status { get; set; }
	}
}
