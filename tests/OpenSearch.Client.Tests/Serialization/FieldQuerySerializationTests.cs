using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for field-keyed leaf queries — those serialized as
/// <c>{ "&lt;kind&gt;": { "&lt;field&gt;": { ... } } }</c>. Ported from the query coverage of the
/// opensearch-java and elasticsearch-net clients, adapted to this client's QueryContainer API.
/// Each case asserts the field-keyed shape plus a canonical serialize→deserialize→serialize round-trip.
/// </summary>
public class FieldQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void Term_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Term("status", new TermQuery
		{
			Value = Element("active"),
			Boost = 1.5f,
			CaseInsensitive = true,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "term", "status");
		inner.GetProperty("value").GetString().Should().Be("active");
		inner.GetProperty("boost").GetSingle().Should().Be(1.5f);
		inner.GetProperty("case_insensitive").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Match_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Match("title", new MatchQuery
		{
			Query = Element("quick brown fox"),
			Analyzer = "standard",
			MinimumShouldMatch = "2",
			Boost = 2.0f,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "match", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown fox");
		inner.GetProperty("analyzer").GetString().Should().Be("standard");
		inner.GetProperty("minimum_should_match").GetString().Should().Be("2");
	}

	[Fact]
	public void MatchPhrase_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.MatchPhrase("title", new MatchPhraseQuery
		{
			Query = "quick brown fox",
			Slop = 2,
			Analyzer = "standard",
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "match_phrase", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown fox");
		inner.GetProperty("slop").GetInt32().Should().Be(2);
	}

	[Fact]
	public void MatchBoolPrefix_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.MatchBoolPrefix("title", new MatchBoolPrefixQuery
		{
			Query = "quick brown f",
			MinimumShouldMatch = "3",
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "match_bool_prefix", "title");
		inner.GetProperty("query").GetString().Should().Be("quick brown f");
		inner.GetProperty("minimum_should_match").GetString().Should().Be("3");
	}

	[Fact]
	public void Prefix_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Prefix("user.id", new PrefixQuery
		{
			Value = "ki",
			CaseInsensitive = true,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "prefix", "user.id");
		inner.GetProperty("value").GetString().Should().Be("ki");
		inner.GetProperty("case_insensitive").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Wildcard_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Wildcard("user.id", new WildcardQuery
		{
			Value = "ki*y",
			Boost = 1.2f,
			CaseInsensitive = false,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "wildcard", "user.id");
		inner.GetProperty("value").GetString().Should().Be("ki*y");
		inner.GetProperty("boost").GetSingle().Should().Be(1.2f);
	}

	[Fact]
	public void Regexp_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Regexp("user.id", new RegexpQuery
		{
			Value = "k.*y",
			Flags = "ALL",
			MaxDeterminizedStates = 10000,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "regexp", "user.id");
		inner.GetProperty("value").GetString().Should().Be("k.*y");
		inner.GetProperty("flags").GetString().Should().Be("ALL");
		inner.GetProperty("max_determinized_states").GetInt32().Should().Be(10000);
	}

	[Fact]
	public void Fuzzy_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Fuzzy("user.id", new FuzzyQuery
		{
			Value = Element("ki"),
			Fuzziness = "AUTO",
			MaxExpansions = 50,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "fuzzy", "user.id");
		inner.GetProperty("value").GetString().Should().Be("ki");
		inner.GetProperty("fuzziness").GetString().Should().Be("AUTO");
		inner.GetProperty("max_expansions").GetInt32().Should().Be(50);
	}

	[Fact]
	public void Range_numeric_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Range("age", new RangeQuery
		{
			Gte = 10,
			Lte = 20,
			Boost = 2.0f,
			Relation = RangeRelation.Within,
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "range", "age");
		inner.GetProperty("gte").GetInt32().Should().Be(10);
		inner.GetProperty("lte").GetInt32().Should().Be(20);
	}

	[Fact]
	public void Range_date_math_serializes_field_keyed_and_round_trips()
	{
		// The merged RangeQuery accepts date-math string bounds as well as numbers.
		var query = QueryContainer.Range("timestamp", new RangeQuery
		{
			Gte = "now-1d/d",
			Lte = "now",
			Format = "strict_date_optional_time",
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "range", "timestamp");
		inner.GetProperty("gte").GetString().Should().Be("now-1d/d");
		inner.GetProperty("format").GetString().Should().Be("strict_date_optional_time");
	}
}
