using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SearchResponseExtensionsTests
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
		public int? Price { get; set; }
	}

	// ── Documents() ──

	[Fact]
	public void Documents_ReturnsSourceDocuments()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": { "value": 2, "relation": "eq" },
				"hits": [
					{ "_index": "idx", "_id": "1", "_source": { "title": "First", "price": 10 } },
					{ "_index": "idx", "_id": "2", "_source": { "title": "Second", "price": 20 } }
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var docs = response.Documents();

		docs.Should().HaveCount(2);
		docs[0].Title.Should().Be("First");
		docs[0].Price.Should().Be(10);
		docs[1].Title.Should().Be("Second");
	}

	[Fact]
	public void Documents_SkipsHitsWithNullSource()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": { "value": 2, "relation": "eq" },
				"hits": [
					{ "_index": "idx", "_id": "1", "_source": { "title": "Has source" } },
					{ "_index": "idx", "_id": "2" }
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var docs = response.Documents();

		docs.Should().HaveCount(1);
		docs[0].Title.Should().Be("Has source");
	}

	[Fact]
	public void Documents_EmptyResponse_ReturnsEmptyList()
	{
		var response = new SearchResponse<TestDoc>();
		response.Documents().Should().BeEmpty();
	}

	// ── Total() ──

	[Fact]
	public void Total_ReturnsTotalHitsValue()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": { "value": 42, "relation": "eq" },
				"hits": []
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		response.Total().Should().Be(42);
	}

	[Fact]
	public void Total_NullHits_ReturnsZero()
	{
		var response = new SearchResponse<TestDoc>();
		response.Total().Should().Be(0);
	}

	// ── IsValid() ──

	[Fact]
	public void IsValid_NoShardFailures_ReturnsTrue()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 5, "successful": 5, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] }
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		response.IsValid.Should().BeTrue();
	}

	[Fact]
	public void IsValid_WithShardFailures_ReturnsFalse()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 5, "successful": 3, "skipped": 0, "failed": 2 },
			"hits": { "hits": [] }
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		response.IsValid.Should().BeFalse();
	}

	[Fact]
	public void ShardFailure_Reason_DeserializesAsErrorCause()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": {
				"total": 5, "successful": 3, "skipped": 0, "failed": 2,
				"failures": [{
					"shard": 0,
					"index": "my-index",
					"node": "node-1",
					"reason": {
						"type": "query_shard_exception",
						"reason": "No mapping found for [timestamp]",
						"caused_by": {
							"type": "illegal_argument_exception",
							"reason": "No mapping found for [timestamp]"
						}
					}
				}]
			},
			"hits": { "hits": [] }
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		response.IsValid.Should().BeFalse();
		response.Shards!.Failures.Should().HaveCount(1);

		var failure = response.Shards.Failures![0];
		failure.Index.Should().Be("my-index");
		failure.Node.Should().Be("node-1");
		failure.Shard.Should().Be(0);

		// Reason is OpenSearch.Net.ErrorCause (type override from codegen)
		failure.Reason.Should().NotBeNull();
		failure.Reason!.Type.Should().Be("query_shard_exception");
		failure.Reason.Reason.Should().Be("No mapping found for [timestamp]");
		failure.Reason.CausedBy.Should().NotBeNull();
		failure.Reason.CausedBy!.Type.Should().Be("illegal_argument_exception");
	}

	[Fact]
	public void IsValid_NullShards_AndNoApiCall_ReturnsTrue()
	{
		// A manually constructed response with no ApiCall and no Shards
		// is considered valid (no transport details to indicate failure).
		var response = new SearchResponse<TestDoc>();
		response.IsValid.Should().BeTrue();
	}

	// ── Aggs() ──

	[Fact]
	public void Aggs_ReturnsAggregateDictionary()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"avg_price": { "value": 9.99 }
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var aggs = response.Aggs();

		aggs.Average("avg_price").Should().Be(9.99);
	}

	// ── Suggestions() ──

	[Fact]
	public void Suggestions_ReturnsTypedSuggestDictionary()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"suggest": {
				"my_suggest": [
					{
						"text": "tset",
						"offset": 0,
						"length": 4,
						"options": [
							{ "text": "test", "score": 0.75, "freq": 5 }
						]
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var suggestions = response.Suggestions();

		var termEntries = suggestions.GetTerm("my_suggest");
		termEntries.Should().NotBeNull();
		termEntries.Should().HaveCount(1);
		termEntries![0].Text.Should().Be("tset");
		termEntries[0].Options.Should().HaveCount(1);
		termEntries[0].Options[0].Text.Should().Be("test");
		termEntries[0].Options[0].Score.Should().Be(0.75);
		termEntries[0].Options[0].Freq.Should().Be(5);
	}
}
