using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class MsearchResponseTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	private sealed class TestDoc
	{
		public string? Title { get; set; }
	}

	[Fact]
	public void Deserialize_MsearchResponse_WithTotalHits()
	{
		var json = """
		{
			"took": 5,
			"responses": [
				{
					"took": 2,
					"timed_out": false,
					"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
					"hits": {
						"total": { "value": 10, "relation": "eq" },
						"max_score": 1.0,
						"hits": [
							{ "_index": "books", "_id": "1", "_source": { "title": "First" } }
						]
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MsearchResponse>(json, JsonOptions)!;

		response.Took.Should().Be(5);
		response.Responses.Should().HaveCount(1);

		var item = response.Responses![0];
		item.Hits.Should().NotBeNull();
		item.Hits!.Total.Should().NotBeNull();
		item.Hits.Total!.Value.Should().Be(10);
		item.Hits.Total.Relation.Should().Be("eq");
	}

	[Fact]
	public void GetHits_DeserializesIntoTypedHits()
	{
		var json = """
		{
			"took": 5,
			"responses": [
				{
					"took": 2,
					"timed_out": false,
					"hits": {
						"total": { "value": 1, "relation": "eq" },
						"hits": [
							{ "_index": "books", "_id": "1", "_score": 1.0, "_source": { "title": "Test Book" } }
						]
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MsearchResponse>(json, JsonOptions)!;
		var hits = response.Responses![0].GetHits<TestDoc>();

		hits.Should().HaveCount(1);
		hits[0].Index.Should().Be("books");
		hits[0].Id.Should().Be("1");
		hits[0].Source.Should().NotBeNull();
		hits[0].Source!.Title.Should().Be("Test Book");
	}

	[Fact]
	public void GetAggregations_ParsesRawAggregations()
	{
		var json = """
		{
			"took": 5,
			"responses": [
				{
					"took": 2,
					"timed_out": false,
					"hits": { "hits": [] },
					"aggregations": {
						"avg_price": { "value": 19.99 }
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MsearchResponse>(json, JsonOptions)!;
		var aggs = response.Responses![0].GetAggregations();

		aggs.Average("avg_price").Should().Be(19.99);
	}

	[Fact]
	public void GetHits_EmptyResponse_ReturnsEmptyList()
	{
		var json = """
		{
			"took": 1,
			"responses": [
				{
					"took": 1,
					"timed_out": false,
					"hits": { "hits": [] }
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MsearchResponse>(json, JsonOptions)!;
		response.Responses![0].GetHits<TestDoc>().Should().BeEmpty();
	}

	[Fact]
	public void GetAggregations_NullAggregations_ReturnsEmptyDictionary()
	{
		var json = """
		{
			"took": 1,
			"responses": [
				{
					"took": 1,
					"timed_out": false,
					"hits": { "hits": [] }
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MsearchResponse>(json, JsonOptions)!;
		var aggs = response.Responses![0].GetAggregations();
		aggs.Average("missing").Should().BeNull();
	}
}
