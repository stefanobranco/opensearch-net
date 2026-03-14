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

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// Create an alias
		var putAliasResponse = Client.Indices.PutAlias(new PutAliasRequest
		{
			Index = index,
			Name = aliasName
		});

		putAliasResponse.Acknowledged.Should().BeTrue();

		// Verify alias exists
		var existsAliasResponse = Client.Indices.ExistsAlias(new ExistsAliasRequest
		{
			Name = aliasName,
			Index = index
		});

		existsAliasResponse.Exists.Should().BeTrue();

		// Get alias
		var getAliasResponse = Client.Indices.GetAlias(new GetAliasRequest
		{
			Index = index,
			Name = aliasName
		});

		getAliasResponse.Should().ContainKey(index);
		getAliasResponse[index].Aliases.Should().ContainKey(aliasName);
	}

	[SkipIfNoCluster]
	public void SearchViaAlias()
	{
		var index = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

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
		Client.Indices.PutAlias(new PutAliasRequest { Index = index, Name = aliasName });

		// Search via alias name
		var searchResponse = Client.Core.Search<AliasDoc>(new SearchRequest
		{
			Index = aliasName,
			Size = 10
		});

		searchResponse.Hits.Should().NotBeNull();
		searchResponse.Hits!.Hits.Should().HaveCount(1);
		searchResponse.Hits.Hits![0].Source!.Name.Should().Be("Via alias");
	}

	[SkipIfNoCluster]
	public void UpdateAliases_AddAndRemove()
	{
		var index1 = UniqueIndex("alias");
		var index2 = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index1 });
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index2 });

		// Add alias to index1
		Client.Indices.PutAlias(new PutAliasRequest { Index = index1, Name = aliasName });

		// Verify alias points to index1
		var existsOnIndex1 = Client.Indices.ExistsAlias(new ExistsAliasRequest { Name = aliasName, Index = index1 });
		existsOnIndex1.Exists.Should().BeTrue();

		// Use UpdateAliases to atomically move alias from index1 to index2
		var updateResponse = Client.Indices.UpdateAliases(new UpdateAliasesRequest
		{
			Actions =
			[
				new IndexAction { Remove = new RemoveAction { Index = index1, Alias = aliasName } },
				new IndexAction { Add = new AddAction { Index = index2, Alias = aliasName } },
			]
		});

		updateResponse.Acknowledged.Should().BeTrue();

		// Verify alias no longer exists on index1
		var existsAfterRemove = Client.Indices.ExistsAlias(new ExistsAliasRequest { Name = aliasName, Index = index1 });
		existsAfterRemove.Exists.Should().BeFalse();

		// Verify alias exists on index2
		var existsOnIndex2 = Client.Indices.ExistsAlias(new ExistsAliasRequest { Name = aliasName, Index = index2 });
		existsOnIndex2.Exists.Should().BeTrue();
	}

	[SkipIfNoCluster]
	public void DeleteAlias()
	{
		var index = UniqueIndex("alias");
		var aliasName = $"alias-{Guid.NewGuid():N}";

		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });
		Client.Indices.PutAlias(new PutAliasRequest { Index = index, Name = aliasName });

		// Verify exists
		var existsBefore = Client.Indices.ExistsAlias(new ExistsAliasRequest { Name = aliasName, Index = index });
		existsBefore.Exists.Should().BeTrue();

		// Delete alias
		var deleteResponse = Client.Indices.DeleteAlias(new DeleteAliasRequest
		{
			Index = index,
			Name = aliasName
		});

		deleteResponse.Acknowledged.Should().BeTrue();

		// Verify deleted
		var existsAfter = Client.Indices.ExistsAlias(new ExistsAliasRequest { Name = aliasName, Index = index });
		existsAfter.Exists.Should().BeFalse();
	}

	private sealed class AliasDoc
	{
		public string? Name { get; set; }
	}
}
