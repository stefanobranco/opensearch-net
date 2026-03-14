using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client.Core;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

public class MsearchTests : IntegrationTestBase
{
	[SkipIfNoCluster]
	public void MsearchWithTwoQueries()
	{
		var index = UniqueIndex();
		Client.Indices.Create(new OpenSearch.Client.Indices.CreateRequest { Index = index });

		// Bulk index test data
		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<MsearchDoc> { Document = new MsearchDoc { Name = "Alpha", Category = "A" }, Id = "1" },
				new BulkIndexOperation<MsearchDoc> { Document = new MsearchDoc { Name = "Beta", Category = "B" }, Id = "2" },
				new BulkIndexOperation<MsearchDoc> { Document = new MsearchDoc { Name = "Gamma", Category = "A" }, Id = "3" },
			]
		});

		// Multi-search: first query matches category A (2 docs), second matches category B (1 doc)
		var msearchResponse = Client.Core.Msearch(new MsearchRequest
		{
			Searches =
			[
				new MsearchItem
				{
					Header = new MsearchHeader { Index = index },
					Body = new MsearchBody
					{
						Query = JsonSerializer.SerializeToElement(new { match = new { category = "A" } }),
						Size = 10
					}
				},
				new MsearchItem
				{
					Header = new MsearchHeader { Index = index },
					Body = new MsearchBody
					{
						Query = JsonSerializer.SerializeToElement(new { match = new { category = "B" } }),
						Size = 10
					}
				}
			]
		});

		msearchResponse.Responses.Should().NotBeNull();
		msearchResponse.Responses.Should().HaveCount(2);

		// First query: 2 hits (Alpha + Gamma)
		msearchResponse.Responses![0].Error.Should().BeNull();
		msearchResponse.Responses[0].Hits.Should().NotBeNull();
		msearchResponse.Responses[0].Hits!.Hits.Should().HaveCount(2);

		// Second query: 1 hit (Beta)
		msearchResponse.Responses[1].Error.Should().BeNull();
		msearchResponse.Responses[1].Hits.Should().NotBeNull();
		msearchResponse.Responses[1].Hits!.Hits.Should().HaveCount(1);
	}

	private sealed class MsearchDoc
	{
		public string? Name { get; set; }
		public string? Category { get; set; }
	}
}
