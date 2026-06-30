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
		var query = Query(q => q.Term(d => d.Status!, t => t.Value(Element("active")).Boost(1.5f)));

		var inner = AssertFieldKeyed(query, "term", "status");
		inner.GetProperty("value").GetString().Should().Be("active");
		inner.GetProperty("boost").GetSingle().Should().Be(1.5f);
	}

	[Fact]
	public void Match_via_field_expression_resolves_field_and_serializes()
	{
		var query = Query(q => q.Match(d => d.Title!, m => m.Query(Element("quick brown fox")).Analyzer("standard")));

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
}
