using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for score-modifying compound queries that wrap an inner
/// <see cref="QueryContainer"/>. Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class ScoringQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void ScriptScore_serializes_with_query_and_script_and_round_trips()
	{
		var query = QueryContainer.ScriptScore(new ScriptScoreQuery
		{
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
			Script = new Script { Source = "_score * doc['multiplier'].value", Lang = "painless" },
			MinScore = 1.0f,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("script_score", out var inner).Should().BeTrue();
		inner.GetProperty("query").TryGetProperty("match_all", out _).Should().BeTrue();
		inner.GetProperty("script").GetProperty("source").GetString().Should().Be("_score * doc['multiplier'].value");
		inner.GetProperty("min_score").GetSingle().Should().Be(1.0f);
	}
}
