using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for joining queries (nested / parent-child), which embed a child
/// <see cref="QueryContainer"/>. Ported from the opensearch-java / elasticsearch-net query coverage.
/// </summary>
public class JoiningQuerySerializationTests : SerializationTestBase
{
	[Fact]
	public void Nested_serializes_with_child_query_and_round_trips()
	{
		var query = QueryContainer.Nested(new NestedQuery
		{
			Path = "comments",
			Query = QueryContainer.Match("comments.text", new MatchQuery { Query = Element("great") }),
			ScoreMode = ChildScoreMode.Avg,
			IgnoreUnmapped = true,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("nested", out var inner).Should().BeTrue();
		inner.GetProperty("path").GetString().Should().Be("comments");
		inner.GetProperty("query").TryGetProperty("match", out _).Should().BeTrue();
		inner.GetProperty("score_mode").GetString().Should().Be("avg");
	}

	[Fact]
	public void HasChild_serializes_and_round_trips()
	{
		var query = QueryContainer.HasChild(new HasChildQuery
		{
			Type = "comment",
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
			MinChildren = 1,
			MaxChildren = 10,
			ScoreMode = ChildScoreMode.Max,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("has_child", out var inner).Should().BeTrue();
		inner.GetProperty("type").GetString().Should().Be("comment");
		inner.GetProperty("min_children").GetInt32().Should().Be(1);
		inner.GetProperty("max_children").GetInt32().Should().Be(10);
		inner.GetProperty("score_mode").GetString().Should().Be("max");
		inner.GetProperty("query").TryGetProperty("match_all", out _).Should().BeTrue();
	}

	[Fact]
	public void HasParent_serializes_and_round_trips()
	{
		var query = QueryContainer.HasParent(new HasParentQuery
		{
			ParentType = "blog",
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
			Score = true,
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("has_parent", out var inner).Should().BeTrue();
		inner.GetProperty("parent_type").GetString().Should().Be("blog");
		inner.GetProperty("score").GetBoolean().Should().BeTrue();
		inner.GetProperty("query").TryGetProperty("match_all", out _).Should().BeTrue();
	}

	[Fact]
	public void ParentId_serializes_and_round_trips()
	{
		var query = QueryContainer.ParentId(new ParentIdQuery
		{
			Id = "1",
			Type = "comment",
		});

		var root = AssertRoundTrips(query);
		root.TryGetProperty("parent_id", out var inner).Should().BeTrue();
		inner.GetProperty("id").GetString().Should().Be("1");
		inner.GetProperty("type").GetString().Should().Be("comment");
	}
}
