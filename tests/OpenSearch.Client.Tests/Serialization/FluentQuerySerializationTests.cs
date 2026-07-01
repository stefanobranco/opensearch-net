using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// End-to-end fixtures for the strongly-typed fluent query API as users actually write it:
/// <c>new SearchRequestDescriptor&lt;TDoc&gt;().Query(q =&gt; q.Term(d =&gt; d.Field, t =&gt; t.Value(...)))</c>.
/// Unlike the value-type fixtures, these exercise the per-query descriptors, the descriptor→query
/// cast operators, and <c>Field</c>-expression resolution (<c>FieldExpressionVisitor</c>, incl. nested
/// snake_casing) — and serialize through the production serializer. A green test here means the
/// typed fluent surface produces correct wire JSON, not just that the value objects do.
/// </summary>
public class FluentQuerySerializationTests : SerializationTestBase
{
	private sealed class Doc
	{
		public string? Status { get; set; }
		public string? Title { get; set; }
		public int Age { get; set; }
		public Meta? Meta { get; set; }
	}

	private sealed class Meta
	{
		public System.DateTime PublishedAt { get; set; }
	}

	/// <summary>Builds a search request via the fluent descriptor and returns its serialized <c>query</c>.</summary>
	private static JsonElement Query(System.Action<QueryContainerDescriptor<Doc>> build)
	{
		SearchRequest request = new SearchRequestDescriptor<Doc>().Query(build);
		return Parse(Serialize(request)).GetProperty("query");
	}

	[Fact]
	public void Term_via_field_expression_resolves_field_and_serializes()
	{
		var query = Query(q => q.Term(d => d.Status!, t => t.Value("active").Boost(1.5f)));

		var inner = AssertFieldKeyed(query, "term", "status");
		inner.GetProperty("value").GetString().Should().Be("active");
		inner.GetProperty("boost").GetSingle().Should().Be(1.5f);
	}

	[Fact]
	public void Match_via_field_expression_resolves_field_and_serializes()
	{
		var query = Query(q => q.Match(d => d.Title!, m => m.Query("quick brown fox").Analyzer("standard")));

		var inner = AssertFieldKeyed(query, "match", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown fox");
		inner.GetProperty("analyzer").GetString().Should().Be("standard");
	}

	[Fact]
	public void Range_via_field_expression_uses_typed_RangeQuery()
	{
		var query = Query(q => q.Range(d => d.Age, r => r.Gte(10).Lte(20)));

		var inner = AssertFieldKeyed(query, "range", "age");
		inner.GetProperty("gte").GetInt32().Should().Be(10);
		inner.GetProperty("lte").GetInt32().Should().Be(20);
	}

	[Fact]
	public void Wildcard_via_field_expression_resolves_field_and_serializes()
	{
		var query = Query(q => q.Wildcard(d => d.Status!, w => w.Value("act*")));

		var inner = AssertFieldKeyed(query, "wildcard", "status");
		inner.GetProperty("value").GetString().Should().Be("act*");
	}

	[Fact]
	public void Prefix_via_field_expression_resolves_field_and_serializes()
	{
		var query = Query(q => q.Prefix(d => d.Title!, p => p.Value("qui")));

		var inner = AssertFieldKeyed(query, "prefix", "title");
		inner.GetProperty("value").GetString().Should().Be("qui");
	}

	[Fact]
	public void Exists_via_field_expression_resolves_field()
	{
		var query = Query(q => q.Exists(d => d.Title!));

		query.TryGetProperty("exists", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("title");
	}

	[Fact]
	public void Nested_field_expression_resolves_to_dotted_snake_case_path()
	{
		// Exercises FieldExpressionVisitor on a nested member: Meta.PublishedAt → "meta.published_at".
		var query = Query(q => q.Exists(d => d.Meta!.PublishedAt));

		query.TryGetProperty("exists", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("meta.published_at");
	}

	[Fact]
	public void MatchPhrase_via_field_expression_serializes()
	{
		var query = Query(q => q.MatchPhrase(d => d.Title!, m => m.Query("quick brown fox").Slop(2)));

		var inner = AssertFieldKeyed(query, "match_phrase", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown fox");
		inner.GetProperty("slop").GetInt32().Should().Be(2);
	}

	[Fact]
	public void MatchPhrasePrefix_via_field_expression_serializes()
	{
		var query = Query(q => q.MatchPhrasePrefix(d => d.Title!, m => m.Query("quick br").MaxExpansions(50)));

		var inner = AssertFieldKeyed(query, "match_phrase_prefix", "title");
		inner.GetProperty("query").GetString().Should().Be("quick br");
		inner.GetProperty("max_expansions").GetInt32().Should().Be(50);
	}

	[Fact]
	public void Regexp_via_field_expression_serializes()
	{
		var query = Query(q => q.Regexp(d => d.Status!, r => r.Value("a.*e").Flags("ALL")));

		var inner = AssertFieldKeyed(query, "regexp", "status");
		inner.GetProperty("value").GetString().Should().Be("a.*e");
		inner.GetProperty("flags").GetString().Should().Be("ALL");
	}

	[Fact]
	public void Fuzzy_via_field_expression_serializes()
	{
		var query = Query(q => q.Fuzzy(d => d.Status!, f => f.Value("activ").Fuzziness("AUTO")));

		var inner = AssertFieldKeyed(query, "fuzzy", "status");
		inner.GetProperty("value").GetString().Should().Be("activ");
		inner.GetProperty("fuzziness").GetString().Should().Be("AUTO");
	}

	[Fact]
	public void Terms_via_field_expression_serializes()
	{
		var query = Query(q => q.Terms(d => d.Status!, "active", "pending"));

		var inner = AssertFieldKeyed(query, "terms", "status");
		inner.GetArrayLength().Should().Be(2);
		inner[0].GetString().Should().Be("active");
	}

	[Fact]
	public void MatchAll_fluent_serializes()
	{
		var query = Query(q => q.MatchAll(m => m.Boost(1.0f)));

		query.TryGetProperty("match_all", out var inner).Should().BeTrue();
		inner.GetProperty("boost").GetSingle().Should().Be(1.0f);
	}

	[Fact]
	public void QueryString_fluent_serializes()
	{
		var query = Query(q => q.QueryString(qs => qs.Query("(new york) OR london").DefaultField("content")));

		query.TryGetProperty("query_string", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("(new york) OR london");
		inner.GetProperty("default_field").GetString().Should().Be("content");
	}

	[Fact]
	public void Bool_fluent_with_nested_field_expressions_serializes()
	{
		// Compound fluent: Bool clauses each contain field-expression leaf queries —
		// validates BoolQueryDescriptor<T> plus nested FieldExpressionVisitor resolution.
		var query = Query(q => q.Bool(b => b
			.Must(m => m.Term(d => d.Status!, t => t.Value("active")))
			.Filter(f => f.Range(d => d.Age, r => r.Gte(18)))
			.MinimumShouldMatch("1")));

		query.TryGetProperty("bool", out var inner).Should().BeTrue();
		AssertFieldKeyed(inner.GetProperty("must")[0], "term", "status");
		inner.GetProperty("filter")[0].GetProperty("range").GetProperty("age").GetProperty("gte").GetInt32().Should().Be(18);
		inner.GetProperty("minimum_should_match").GetString().Should().Be("1");
	}

	[Fact]
	public void Nested_fluent_with_inner_field_expression_serializes()
	{
		var query = Query(q => q.Nested(n => n
			.Path("comments")
			.ScoreMode(ChildScoreMode.Avg)
			.Query(nq => nq.Match(d => d.Title!, m => m.Query("great")))));

		query.TryGetProperty("nested", out var inner).Should().BeTrue();
		inner.GetProperty("path").GetString().Should().Be("comments");
		inner.GetProperty("score_mode").GetString().Should().Be("avg");
		AssertFieldKeyed(inner.GetProperty("query"), "match", "title");
	}

	// ── Newly-generated variants (previously unreachable on the generic descriptor) ──

	[Fact]
	public void SpanTerm_generated_field_expression_serializes()
	{
		var query = Query(q => q.SpanTerm(d => d.Status!, s => s.Value("active")));

		var inner = AssertFieldKeyed(query, "span_term", "status");
		inner.GetProperty("value").GetString().Should().Be("active");
	}

	[Fact]
	public void Common_generated_field_expression_serializes()
	{
		var query = Query(q => q.Common(d => d.Title!, c => c.Query("nelly the elephant").CutoffFrequency(0.001f)));

		var inner = AssertFieldKeyed(query, "common", "title");
		inner.GetProperty("query").GetString().Should().Be("nelly the elephant");
	}

	[Fact]
	public void Wrapper_generated_action_serializes()
	{
		var query = Query(q => q.Wrapper(w => w.Query("eyJ0ZXJtIjoge319")));

		query.TryGetProperty("wrapper", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("eyJ0ZXJtIjoge319");
	}

	[Fact]
	public void Type_generated_action_serializes()
	{
		var query = Query(q => q.Type(t => t.Value("_doc")));

		query.TryGetProperty("type", out var inner).Should().BeTrue();
		inner.GetProperty("value").GetString().Should().Be("_doc");
	}

	[Fact]
	public void DisMax_generated_value_form_is_reachable()
	{
		var query = Query(q => q.DisMax(new DisMaxQuery { TieBreaker = 0.5f }));

		query.TryGetProperty("dis_max", out var inner).Should().BeTrue();
		inner.GetProperty("tie_breaker").GetSingle().Should().Be(0.5f);
	}

	// ── Nested Field expressions inside compound queries (full TDocument threading) ──

	[Fact]
	public void DisMax_fluent_nests_field_expressions()
	{
		var query = Query(q => q.DisMax(d => d
			.TieBreaker(0.5f)
			.Queries(
				sq => sq.Term(x => x.Status!, t => t.Value("active")),
				sq => sq.Match(x => x.Title!, m => m.Query("brown fox")))));

		query.TryGetProperty("dis_max", out var inner).Should().BeTrue();
		var queries = inner.GetProperty("queries");
		queries.GetArrayLength().Should().Be(2);
		AssertFieldKeyed(queries[0], "term", "status");
		AssertFieldKeyed(queries[1], "match", "title");
	}

	[Fact]
	public void HasChild_fluent_nests_field_expression_query()
	{
		var query = Query(q => q.HasChild(h => h
			.Type("comment")
			.Query(cq => cq.Term(x => x.Status!, t => t.Value("active")))));

		query.TryGetProperty("has_child", out var inner).Should().BeTrue();
		inner.GetProperty("type").GetString().Should().Be("comment");
		AssertFieldKeyed(inner.GetProperty("query"), "term", "status");
	}

	[Fact]
	public void SpanNear_fluent_nests_field_expression_span_terms()
	{
		var query = Query(q => q.SpanNear(sn => sn
			.Slop(2)
			.Clauses(
				sq => sq.SpanTerm(x => x.Status!, s => s.Value("active")),
				sq => sq.SpanTerm(x => x.Title!, s => s.Value("fox")))));

		query.TryGetProperty("span_near", out var inner).Should().BeTrue();
		inner.GetProperty("slop").GetInt32().Should().Be(2);
		var clauses = inner.GetProperty("clauses");
		clauses.GetArrayLength().Should().Be(2);
		AssertFieldKeyed(clauses[0], "span_term", "status");
		AssertFieldKeyed(clauses[1], "span_term", "title");
	}
}
