using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

/// <summary>
/// Exercises the async pipeline end-to-end against a real cluster — transport send, request
/// serialization, NDJSON streaming writes, and response deserialization all have distinct async
/// code paths that the synchronous tests never touch. Production consumers are async-first.
/// </summary>
public class AsyncPipelineTests : IntegrationTestBase
{
	private sealed class AsyncDoc
	{
		public string? Name { get; set; }
		public int Value { get; set; }
	}

	[SkipIfNoCluster]
	public async Task FullDocumentLifecycle_ThroughAsyncOverloads()
	{
		var index = UniqueIndex("async");

		var create = await Client.Indices.CreateAsync(new CreateIndexRequest { Index = index });
		create.IsValid.Should().BeTrue(create.DebugInformation);

		// Bulk NDJSON — WriteToAsync path.
		var bulk = await Client.Core.BulkAsync(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations =
			[
				new BulkIndexOperation<AsyncDoc> { Document = new AsyncDoc { Name = "alpha", Value = 1 }, Id = "1" },
				new BulkIndexOperation<AsyncDoc> { Document = new AsyncDoc { Name = "beta", Value = 2 }, Id = "2" },
				new BulkIndexOperation<AsyncDoc> { Document = new AsyncDoc { Name = "gamma", Value = 3 }, Id = "3" },
			],
		});
		bulk.IsValid.Should().BeTrue(bulk.DebugInformation);
		bulk.Errors.Should().BeFalse();

		var search = await Client.Core.SearchAsync<AsyncDoc>(new SearchRequest
		{
			Index = [index],
			Query = QueryContainer.Match("name", new MatchQuery { Query = "beta" }),
		});
		search.IsValid.Should().BeTrue(search.DebugInformation);
		search.Hits!.Hits.Should().HaveCount(1);
		search.Hits.Hits![0].Source!.Value.Should().Be(2);

		var get = await Client.Core.GetAsync<AsyncDoc>(new GetRequest { Index = index, Id = "3" });
		get.IsValid.Should().BeTrue(get.DebugInformation);
		get.Source!.Name.Should().Be("gamma");

		// Single-document index — the document-body async write path.
		var indexResponse = await Client.Core.IndexAsync(new IndexRequest
		{
			Index = index,
			Id = "4",
			Refresh = "true",
			Body = new AsyncDoc { Name = "delta", Value = 4 },
		});
		indexResponse.IsValid.Should().BeTrue(indexResponse.DebugInformation);

		var delete = await Client.Core.DeleteAsync(new DeleteRequest { Index = index, Id = "1", Refresh = "true" });
		delete.IsValid.Should().BeTrue(delete.DebugInformation);

		var count = await Client.Core.CountAsync(new CountRequest { Index = [index] });
		count.IsValid.Should().BeTrue(count.DebugInformation);
		count.Count.Should().Be(3);
	}

	[SkipIfNoCluster]
	public async Task MsearchAndScroll_ThroughAsyncOverloads()
	{
		var index = UniqueIndex("async-ms");

		await Client.Indices.CreateAsync(new CreateIndexRequest { Index = index });
		await Client.Core.BulkAsync(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations = Enumerable.Range(0, 15)
				.Select(BulkOperation (i) => new BulkIndexOperation<AsyncDoc>
				{
					Document = new AsyncDoc { Name = $"doc{i}", Value = i },
					Id = i.ToString(),
				})
				.ToList(),
		});

		// Msearch NDJSON — async streaming write of header+body pairs.
		var msearch = await Client.Core.MsearchAsync(new MsearchRequest
		{
			Searches =
			[
				new MsearchItem
				{
					Header = new MsearchHeader { Index = index },
					Body = new MsearchBody { Size = 5 },
				},
			],
		});
		msearch.IsValid.Should().BeTrue(msearch.DebugInformation);
		msearch.Responses.Should().HaveCount(1);

		// Scroll cursor through the async overloads.
		var first = await Client.Core.SearchAsync<AsyncDoc>(new SearchRequest
		{
			Index = [index],
			Size = 10,
			Scroll = "1m",
		});
		first.IsValid.Should().BeTrue(first.DebugInformation);

		var scroll = await Client.Core.ScrollAsync<AsyncDoc>(
			new ScrollRequest { ScrollId = first.ScrollId, Scroll = "1m" });
		scroll.IsValid.Should().BeTrue(scroll.DebugInformation);
		scroll.Hits!.Hits.Should().HaveCount(5);

		await Client.Core.ClearScrollAsync(new ClearScrollRequest { ScrollId = [scroll.ScrollId ?? first.ScrollId!] });
	}
}
