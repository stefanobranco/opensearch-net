using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for specialized queries: term-level vector/script queries, document
/// queries, and the remaining field-keyed full-text variants. Ported from the opensearch-java /
/// elasticsearch-net query coverage.
/// </summary>
public class SpecializedQuerySerializationTests : SerializationTestBase
{
	// ── Field-keyed ──

	[Fact]
	public void MatchPhrasePrefix_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.MatchPhrasePrefix("title", new MatchPhrasePrefixQuery
		{
			Query = "quick brown f",
			MaxExpansions = 50,
			Slop = 2,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "match_phrase_prefix", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown f");
		inner.GetProperty("max_expansions").GetInt32().Should().Be(50);
	}

	[Fact]
	public void Common_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Common("body", new CommonTermsQuery
		{
			Query = "nelly the elephant",
			CutoffFrequency = 0.001f,
			MinimumShouldMatch = "2",
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "common", "body");
		inner.GetProperty("query").GetString().Should().Be("nelly the elephant");
		inner.GetProperty("minimum_should_match").GetString().Should().Be("2");
	}

	[Fact]
	public void Knn_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Knn("image_vector", new KnnQuery
		{
			Vector = [0.1f, 0.2f, 0.3f],
			K = 10,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "knn", "image_vector");
		inner.GetProperty("vector").GetArrayLength().Should().Be(3);
		inner.GetProperty("k").GetInt32().Should().Be(10);
	}

	[Fact]
	public void Neural_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Neural("passage_embedding", new NeuralQuery
		{
			QueryText = "wild west",
			ModelId = "aVeIf4kBdYv",
			K = 5,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "neural", "passage_embedding");
		inner.GetProperty("query_text").GetString().Should().Be("wild west");
		inner.GetProperty("model_id").GetString().Should().Be("aVeIf4kBdYv");
		inner.GetProperty("k").GetInt32().Should().Be(5);
	}

	[Fact]
	public void TermsSet_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.TermsSet("programming_languages", new TermsSetQuery
		{
			Terms = ["c++", "java", "php"],
			MinimumShouldMatchField = "required_matches",
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "terms_set", "programming_languages");
		inner.GetProperty("terms").GetArrayLength().Should().Be(3);
		inner.GetProperty("minimum_should_match_field").GetString().Should().Be("required_matches");
	}

	// ── Object-valued ──

	[Fact]
	public void Script_serializes_and_round_trips()
	{
		var query = QueryContainer.Script(new ScriptQuery
		{
			Script = new Script { Source = "doc['amount'].value > 100", Lang = "painless" },
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("script", out var inner).Should().BeTrue();
		var script = inner.GetProperty("script");
		script.GetProperty("source").GetString().Should().Be("doc['amount'].value > 100");
		script.GetProperty("lang").GetString().Should().Be("painless");
	}

	[Fact]
	public void Wrapper_serializes_and_round_trips()
	{
		var query = QueryContainer.Wrapper(new WrapperQuery
		{
			Query = "eyJ0ZXJtIjogeyJ1c2VyLmlkIjogImtpbWNoeSJ9fQ==",
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("wrapper", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("eyJ0ZXJtIjogeyJ1c2VyLmlkIjogImtpbWNoeSJ9fQ==");
	}

	[Fact]
	public void Type_serializes_and_round_trips()
	{
		var query = QueryContainer.Type(new TypeQuery { Value = "_doc" });

		var root = AssertRoundTrips(query);
		root.TryGetProperty("type", out var inner).Should().BeTrue();
		inner.GetProperty("value").GetString().Should().Be("_doc");
	}

	[Fact]
	public void MoreLikeThis_serializes_and_round_trips()
	{
		var query = QueryContainer.MoreLikeThis(new MoreLikeThisQuery
		{
			Fields = ["title", "description"],
			Like = ["Once upon a time"],
			MinTermFreq = 1,
			MaxQueryTerms = 12,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("more_like_this", out var inner).Should().BeTrue();
		inner.GetProperty("fields").GetArrayLength().Should().Be(2);
		inner.GetProperty("like").GetArrayLength().Should().Be(1);
		inner.GetProperty("min_term_freq").GetInt32().Should().Be(1);
	}

	[Fact]
	public void Percolate_serializes_and_round_trips()
	{
		var query = QueryContainer.Percolate(new PercolateQuery
		{
			Field = "query",
			Document = Element(new { message = "bonsai tree" }),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("percolate", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("query");
		inner.GetProperty("document").GetProperty("message").GetString().Should().Be("bonsai tree");
	}

	[Fact]
	public void RankFeature_serializes_and_round_trips()
	{
		var query = QueryContainer.RankFeature(new RankFeatureQuery
		{
			Field = "pagerank",
			Boost = 1.0f,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("rank_feature", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("pagerank");
	}

	[Fact]
	public void Hybrid_serializes_with_nested_queries_and_round_trips()
	{
		var query = QueryContainer.Hybrid(new HybridQuery
		{
			Queries =
			[
				QueryContainer.Term("status", new TermQuery { Value = "active" }),
				QueryContainer.Match("title", new MatchQuery { Query = "brown fox" }),
			],
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("hybrid", out var inner).Should().BeTrue();
		inner.GetProperty("queries").GetArrayLength().Should().Be(2);
	}
}
