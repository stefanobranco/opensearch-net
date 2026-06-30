using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for the span query family. Span queries compose other span clauses via the
/// <see cref="SpanQuery"/> tagged union; <c>span_multi</c> wraps a multi-term <see cref="QueryContainer"/>.
/// Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class SpanQuerySerializationTests : SerializationTestBase
{
	private static SpanQuery Clause(string field, string value) =>
		SpanQuery.SpanTerm(field, new SpanTermQuery { Value = value });

	[Fact]
	public void SpanTerm_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.SpanTerm("user.id", new SpanTermQuery { Value = "kimchy" });

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "span_term", "user.id");
		inner.GetProperty("value").GetString().Should().Be("kimchy");
	}

	[Fact]
	public void SpanFirst_serializes_and_round_trips()
	{
		var query = QueryContainer.SpanFirst(new SpanFirstQuery
		{
			End = 3,
			Match = Clause("user.id", "kimchy"),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_first", out var inner).Should().BeTrue();
		inner.GetProperty("end").GetInt32().Should().Be(3);
		inner.GetProperty("match").TryGetProperty("span_term", out _).Should().BeTrue();
	}

	[Fact]
	public void SpanNear_serializes_with_clauses_and_round_trips()
	{
		var query = QueryContainer.SpanNear(new SpanNearQuery
		{
			Clauses = [Clause("text", "quick"), Clause("text", "fox")],
			Slop = 12,
			InOrder = false,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_near", out var inner).Should().BeTrue();
		inner.GetProperty("clauses").GetArrayLength().Should().Be(2);
		inner.GetProperty("slop").GetInt32().Should().Be(12);
		inner.GetProperty("in_order").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void SpanOr_serializes_with_clauses_and_round_trips()
	{
		var query = QueryContainer.SpanOr(new SpanOrQuery
		{
			Clauses = [Clause("text", "quick"), Clause("text", "brown"), Clause("text", "fox")],
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_or", out var inner).Should().BeTrue();
		inner.GetProperty("clauses").GetArrayLength().Should().Be(3);
	}

	[Fact]
	public void SpanNot_serializes_with_include_exclude_and_round_trips()
	{
		var query = QueryContainer.SpanNot(new SpanNotQuery
		{
			Include = Clause("text", "fox"),
			Exclude = Clause("text", "red"),
			Pre = 1,
			Post = 1,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_not", out var inner).Should().BeTrue();
		inner.GetProperty("include").TryGetProperty("span_term", out _).Should().BeTrue();
		inner.GetProperty("exclude").TryGetProperty("span_term", out _).Should().BeTrue();
	}

	[Fact]
	public void SpanContaining_serializes_and_round_trips()
	{
		var query = QueryContainer.SpanContaining(new SpanContainingQuery
		{
			Big = Clause("text", "quick"),
			Little = Clause("text", "fox"),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_containing", out var inner).Should().BeTrue();
		inner.GetProperty("big").TryGetProperty("span_term", out _).Should().BeTrue();
		inner.GetProperty("little").TryGetProperty("span_term", out _).Should().BeTrue();
	}

	[Fact]
	public void SpanWithin_serializes_and_round_trips()
	{
		var query = QueryContainer.SpanWithin(new SpanWithinQuery
		{
			Big = Clause("text", "quick"),
			Little = Clause("text", "fox"),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_within", out var inner).Should().BeTrue();
		inner.GetProperty("big").TryGetProperty("span_term", out _).Should().BeTrue();
	}

	[Fact]
	public void SpanMulti_serializes_with_multi_term_and_round_trips()
	{
		var query = QueryContainer.SpanMulti(new SpanMultiTermQuery
		{
			Match = QueryContainer.Prefix("user.id", new PrefixQuery { Value = "ki" }),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("span_multi", out var inner).Should().BeTrue();
		inner.GetProperty("match").TryGetProperty("prefix", out _).Should().BeTrue();
	}

	[Fact]
	public void FieldMaskingSpan_serializes_and_round_trips()
	{
		var query = QueryContainer.FieldMaskingSpan(new SpanFieldMaskingQuery
		{
			Field = "text",
			Query = Clause("text.stems", "fox"),
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("field_masking_span", out var inner).Should().BeTrue();
		inner.GetProperty("field").GetString().Should().Be("text");
		inner.GetProperty("query").TryGetProperty("span_term", out _).Should().BeTrue();
	}
}
