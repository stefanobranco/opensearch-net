using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for non-field-keyed full-text and multi-field queries — serialized as
/// <c>{ "&lt;kind&gt;": { ... } }</c> with <c>fields</c>/<c>query</c> inside rather than a field key.
/// Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class FullTextQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void QueryString_serializes_and_round_trips()
	{
		var query = QueryContainer.QueryString(new QueryStringQuery
		{
			Query = "(new york) OR london",
			DefaultField = "content",
			Fields = ["title^2", "body"],
			AnalyzeWildcard = true,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("query_string", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("(new york) OR london");
		inner.GetProperty("default_field").GetString().Should().Be("content");
		inner.GetProperty("fields").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void SimpleQueryString_serializes_and_round_trips()
	{
		var query = QueryContainer.SimpleQueryString(new SimpleQueryStringQuery
		{
			Query = "foo + bar",
			Fields = ["title", "body"],
			Flags = "OR|AND|PREFIX",
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("simple_query_string", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("foo + bar");
		inner.GetProperty("flags").GetString().Should().Be("OR|AND|PREFIX");
		inner.GetProperty("fields").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void MultiMatch_serializes_and_round_trips()
	{
		var query = QueryContainer.MultiMatch(new MultiMatchQuery
		{
			Query = "quick brown fox",
			Fields = ["title", "body"],
			TieBreaker = 0.3f,
			Slop = 2,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("multi_match", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("quick brown fox");
		inner.GetProperty("fields").GetArrayLength().Should().Be(2);
		inner.GetProperty("tie_breaker").GetSingle().Should().Be(0.3f);
	}

	[Fact]
	public void CombinedFields_serializes_and_round_trips()
	{
		var query = QueryContainer.CombinedFields(new CombinedFieldsQuery
		{
			Query = "database systems",
			Fields = ["title", "abstract"],
			MinimumShouldMatch = "2",
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("combined_fields", out var inner).Should().BeTrue();
		inner.GetProperty("query").GetString().Should().Be("database systems");
		inner.GetProperty("minimum_should_match").GetString().Should().Be("2");
	}

	[Fact]
	public void DisMax_serializes_with_nested_queries_and_round_trips()
	{
		var query = QueryContainer.DisMax(new DisMaxQuery
		{
			TieBreaker = 0.7f,
			Queries =
			[
				QueryContainer.Term("status", new TermQuery { Value = Element("active") }),
				QueryContainer.Match("title", new MatchQuery { Query = Element("brown fox") }),
			],
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("dis_max", out var inner).Should().BeTrue();
		inner.GetProperty("tie_breaker").GetSingle().Should().Be(0.7f);

		var queries = inner.GetProperty("queries");
		queries.GetArrayLength().Should().Be(2);
		queries[0].TryGetProperty("term", out _).Should().BeTrue();
		queries[1].TryGetProperty("match", out _).Should().BeTrue();
	}
}
