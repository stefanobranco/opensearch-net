using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.Client.Indices;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Indices;

public class AliasTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void PutAliasAndGetAlias()
	{
		var index = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new CreateIndexRequest { Index = index });

		// Create an alias
		var putAliasResponse = Client.Indices.PutAlias(new PutAliasIndexRequest
		{
			Index = [index],
			Name = aliasName
		});

		putAliasResponse.Acknowledged.Should().BeTrue();

		// Verify alias exists
		var existsAliasResponse = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest
		{
			Name = [aliasName],
			Index = [index]
		});

		existsAliasResponse.Exists.Should().BeTrue();

		// Get alias
		var getAliasResponse = Client.Indices.GetAlias(new GetAliasIndexRequest
		{
			Index = [index],
			Name = [aliasName]
		});

		getAliasResponse.Should().ContainKey(index);
		getAliasResponse[index].Aliases.Should().ContainKey(aliasName);
	}

	[SkipIfNoCluster]
	public void SearchViaAlias()
	{
		var index = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new CreateIndexRequest { Index = index });

		// Index data
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<AliasDoc> { Document = new AliasDoc { Name = "Via alias" }, Id = "1" },
			]
		});

		// Create alias
		Client.Indices.PutAlias(new PutAliasIndexRequest { Index = [index], Name = aliasName });

		// Search via alias name
		var searchResponse = Client.Core.Search<AliasDoc>(new SearchRequest
		{
			Index = [aliasName],
			Size = 10
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("Via alias");
	}

	[SkipIfNoCluster]
	public void UpdateAliases_RemoveAlias()
	{
		var index1 = UniqueIndex("alias");
		var index2 = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new CreateIndexRequest { Index = index1 });
		Client.Indices.Create(new CreateIndexRequest { Index = index2 });

		// Add alias to index1 via PutAlias
		Client.Indices.PutAlias(new PutAliasIndexRequest { Index = [index1], Name = aliasName });

		// Verify alias points to index1
		var existsOnIndex1 = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest { Name = [aliasName], Index = [index1] });
		existsOnIndex1.Exists.Should().BeTrue();

		// Use UpdateAliases to remove alias from index1
		var updateResponse = Client.Indices.UpdateAliases(new UpdateAliasesIndexRequest
		{
			Actions =
			[
				new IndexAction { Remove = new RemoveAction { Index = index1, Alias = aliasName } },
			]
		});

		updateResponse.Acknowledged.Should().BeTrue();

		// Verify alias no longer exists on index1
		var existsAfterRemove = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest { Name = [aliasName], Index = [index1] });
		existsAfterRemove.Exists.Should().BeFalse();

		// Add alias to index2 via PutAlias
		Client.Indices.PutAlias(new PutAliasIndexRequest { Index = [index2], Name = aliasName });

		// Verify alias exists on index2
		var existsOnIndex2 = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest { Name = [aliasName], Index = [index2] });
		existsOnIndex2.Exists.Should().BeTrue();
	}

	[SkipIfNoCluster]
	public void DeleteAlias()
	{
		var index = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new CreateIndexRequest { Index = index });
		Client.Indices.PutAlias(new PutAliasIndexRequest { Index = [index], Name = aliasName });

		// Verify exists
		var existsBefore = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest { Name = [aliasName], Index = [index] });
		existsBefore.Exists.Should().BeTrue();

		// Delete alias
		var deleteResponse = Client.Indices.DeleteAlias(new DeleteAliasIndexRequest
		{
			Index = [index],
			Name = [aliasName]
		});

		deleteResponse.Acknowledged.Should().BeTrue();

		// Verify deleted
		var existsAfter = Client.Indices.ExistsAlias(new ExistsAliasIndexRequest { Name = [aliasName], Index = [index] });
		existsAfter.Exists.Should().BeFalse();
	}

	private sealed class AliasDoc
	{
		public string? Name { get; set; }
	}
}
