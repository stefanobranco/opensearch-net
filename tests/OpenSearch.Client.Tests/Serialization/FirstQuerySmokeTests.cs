using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// A newcomer's-eye view: builds the kinds of queries the README Quick Start shows, using ONLY the
/// public API a first-time user would reach for — no <c>JsonElement</c>, no <c>SerializeToElement</c>.
/// This is the consumer-perspective test whose absence once let scalar query values sit behind an opaque
/// <c>JsonElement</c> (see ROADMAP C5): every existing fixture was authored by someone who already knew
/// the escape hatch, so none of them exercised the plain first-query path. If a common value field
/// regresses to requiring manual JsonElement wrapping, this file stops compiling.
/// </summary>
public class FirstQuerySmokeTests : SerializationTestBase
{
	private sealed class MyDoc
	{
		public string? Title { get; set; }
	}

	[Fact]
	public void Fluent_quick_start_compiles_and_serializes()
	{
		// The exact fluent form shown in the README / package Quick Start — no JsonElement anywhere.
		SearchRequest request = new SearchRequestDescriptor<MyDoc>()
			.Index("my-index")
			.Query(q => q.Match(f => f.Title!, m => m.Query("opensearch")))
			.Size(10);

		var root = Parse(Serialize(request));
		AssertFieldKeyed(root.GetProperty("query"), "match", "title")
			.GetProperty("query").GetString().Should().Be("opensearch");
	}

	[Fact]
	public void Match_query_on_a_string_needs_no_json_wrapping()
	{
		// The exact shape of the README Quick Start.
		var request = new SearchRequest
		{
			Index = ["my-index"],
			Query = QueryContainer.Match("title", new MatchQuery { Query = "opensearch" }),
			Size = 10,
		};

		var root = Parse(Serialize(request));
		AssertFieldKeyed(root.GetProperty("query"), "match", "title")
			.GetProperty("query").GetString().Should().Be("opensearch");
	}

	[Fact]
	public void Term_value_accepts_string_number_and_bool_directly()
	{
		Parse(Serialize(new TermQuery { Value = "active" })).GetProperty("value").GetString().Should().Be("active");
		Parse(Serialize(new TermQuery { Value = 42 })).GetProperty("value").GetInt32().Should().Be(42);
		Parse(Serialize(new TermQuery { Value = true })).GetProperty("value").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Track_total_hits_accepts_bool_or_int()
	{
		Parse(Serialize(new SearchRequest { TrackTotalHits = true }))
			.GetProperty("track_total_hits").GetBoolean().Should().BeTrue();
		Parse(Serialize(new SearchRequest { TrackTotalHits = 10_000 }))
			.GetProperty("track_total_hits").GetInt32().Should().Be(10_000);
	}

	[Fact]
	public void Terms_agg_include_and_order_are_typed()
	{
		var agg = new TermsAggregationFields
		{
			Field = "category",
			Include = new[] { "electronics", "books" },
			Order = AggregateOrder.CountDescending,
		};

		var root = Parse(Serialize(agg));
		root.GetProperty("include").GetArrayLength().Should().Be(2);
		root.GetProperty("order").GetProperty("_count").GetString().Should().Be("desc");
	}

	[Fact]
	public void More_like_this_accepts_text_and_document_items()
	{
		var mlt = new MoreLikeThisQuery
		{
			Fields = ["title"],
			Like = ["a passage of text", new LikeDocument { Index = "my-index", Id = "1" }],
		};

		var like = Parse(Serialize(mlt)).GetProperty("like");
		like.GetArrayLength().Should().Be(2);
		like[0].GetString().Should().Be("a passage of text");
		like[1].GetProperty("_id").GetString().Should().Be("1");
	}
}
