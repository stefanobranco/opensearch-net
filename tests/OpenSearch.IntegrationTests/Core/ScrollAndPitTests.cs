using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.IntegrationTests.Infrastructure;

namespace OpenSearch.IntegrationTests.Core;

/// <summary>
/// Stateful pagination flows against a real cluster: scroll cursors, search_after, and
/// point-in-time contexts. These are server-side stateful interactions that serialization
/// fixtures cannot cover.
/// </summary>
public class ScrollAndPitTests : IntegrationTestBase
{
	private const int DocCount = 25;

	[SkipIfNoCluster]
	public void ScrollThroughAllDocuments()
	{
		var index = SetupTestData();

		var first = Client.Core.Search<NumberedDoc>(new SearchRequest
		{
			Index = [index],
			Size = 10,
			Scroll = "1m",
			Sort = [SortOptions.Ascending("number")],
		});
		first.IsValid.Should().BeTrue(first.DebugInformation);
		first.ScrollId.Should().NotBeNullOrEmpty();

		var seen = new List<int>();
		seen.AddRange(first.Hits!.Hits!.Select(h => h.Source!.Number));

		var scrollId = first.ScrollId!;
		try
		{
			while (true)
			{
				var page = Client.Core.Scroll<NumberedDoc>(new ScrollRequest { ScrollId = scrollId, Scroll = "1m" });
				page.IsValid.Should().BeTrue(page.DebugInformation);
				if (page.Hits!.Hits!.Count == 0)
					break;

				seen.AddRange(page.Hits.Hits.Select(h => h.Source!.Number));
				scrollId = page.ScrollId ?? scrollId;
			}
		}
		finally
		{
			Client.Core.ClearScroll(new ClearScrollRequest { ScrollId = [scrollId] });
		}

		seen.Should().HaveCount(DocCount);
		seen.Should().OnlyHaveUniqueItems();
	}

	[SkipIfNoCluster]
	public void SearchAfterPagination_CoversAllDocumentsWithoutOverlap()
	{
		var index = SetupTestData();

		var seen = new List<int>();
		List<FieldValue>? after = null;

		while (true)
		{
			var page = Client.Core.Search<NumberedDoc>(new SearchRequest
			{
				Index = [index],
				Size = 10,
				Sort = [SortOptions.Ascending("number")],
				SearchAfter = after,
			});
			page.IsValid.Should().BeTrue(page.DebugInformation);
			if (page.Hits!.Hits!.Count == 0)
				break;

			seen.AddRange(page.Hits.Hits.Select(h => h.Source!.Number));
			// The last hit's sort values feed straight back as the next page's search_after.
			after = page.Hits.Hits[^1].Sort;
		}

		seen.Should().HaveCount(DocCount);
		seen.Should().BeInAscendingOrder();
		seen.Should().OnlyHaveUniqueItems();
	}

	[SkipIfNoCluster]
	public void PitLifecycle_SearchWithinPointInTime()
	{
		var index = SetupTestData();

		var pit = Client.Core.CreatePit(new CreatePitRequest { Index = [index], KeepAlive = "1m" });
		pit.IsValid.Should().BeTrue(pit.DebugInformation);
		pit.PitId.Should().NotBeNullOrEmpty();

		try
		{
			// A PIT search targets no index — the PIT pins the target and the data view.
			var response = Client.Core.Search<NumberedDoc>(new SearchRequest
			{
				Size = 5,
				Pit = new PointInTimeReference { Id = pit.PitId, KeepAlive = "1m" },
				Sort = [SortOptions.Ascending("number")],
			});
			response.IsValid.Should().BeTrue(response.DebugInformation);
			response.Hits!.Hits.Should().HaveCount(5);
			response.Hits.Hits![0].Source!.Number.Should().Be(0);
		}
		finally
		{
			Client.Core.DeleteAllPits(new DeleteAllPitsRequest());
		}
	}

	private string SetupTestData()
	{
		var index = UniqueIndex("scroll");

		Client.Indices.Create(new CreateIndexRequest
		{
			Index = index,
			Settings = new IndexSettings { NumberOfShards = "1" },
			Mappings = new TypeMapping
			{
				Properties = new Dictionary<string, Property>
				{
					["number"] = Property.IntegerNumberProperty(new IntegerNumberProperty()),
				},
			},
		});

		Client.Core.Bulk(new BulkRequest
		{
			Index = index,
			Refresh = "true",
			Operations = Enumerable.Range(0, DocCount)
				.Select(BulkOperation (i) => new BulkIndexOperation<NumberedDoc>
				{
					Document = new NumberedDoc { Number = i },
					Id = i.ToString(),
				})
				.ToList(),
		});

		return index;
	}

	private sealed class NumberedDoc
	{
		public int Number { get; set; }
	}
}
