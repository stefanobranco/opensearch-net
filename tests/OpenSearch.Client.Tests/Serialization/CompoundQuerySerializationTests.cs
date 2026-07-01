using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for the remaining compound/templated queries: <c>function_score</c> (a list
/// of scoring functions over an inner query), field-keyed <c>intervals</c>, and the templated
/// <c>template</c> query. Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class CompoundQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void FunctionScore_serializes_with_functions_and_round_trips()
	{
		var query = QueryContainer.FunctionScore(new FunctionScoreQuery
		{
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
			Functions =
			[
				new FunctionScoreContainer
				{
					Filter = QueryContainer.Term("status", new TermQuery { Value = "active" }),
					Weight = 2.0f,
				},
				new FunctionScoreContainer
				{
					RandomScore = new RandomScoreFunction { Seed = "10", Field = "_seq_no" },
				},
			],
			MaxBoost = 5.0f,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("function_score", out var inner).Should().BeTrue();
		inner.GetProperty("query").TryGetProperty("match_all", out _).Should().BeTrue();
		var functions = inner.GetProperty("functions");
		functions.GetArrayLength().Should().Be(2);
		functions[0].GetProperty("weight").GetSingle().Should().Be(2.0f);
		functions[1].TryGetProperty("random_score", out _).Should().BeTrue();
		inner.GetProperty("max_boost").GetSingle().Should().Be(5.0f);
	}

	[Fact]
	public void Intervals_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Intervals("my_text", new IntervalsQuery
		{
			Match = new IntervalsMatch
			{
				Query = "my favorite food",
				MaxGaps = 0,
				Ordered = true,
			},
		});

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "intervals", "my_text");
		var match = inner.GetProperty("match");
		match.GetProperty("query").GetString().Should().Be("my favorite food");
		match.GetProperty("max_gaps").GetInt32().Should().Be(0);
		match.GetProperty("ordered").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Template_serializes_field_keyed_and_round_trips()
	{
		var query = QueryContainer.Template("inline", (object)"{\"match\":{\"title\":\"search text\"}}");

		var inner = AssertFieldKeyed(AssertRoundTrips(query), "template", "inline");
		inner.GetString().Should().Be("{\"match\":{\"title\":\"search text\"}}");
	}
}
