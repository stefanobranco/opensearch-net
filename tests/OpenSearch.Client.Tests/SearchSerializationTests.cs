using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SearchSerializationTests
{
	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		options.Converters.Add(new JsonEnumConverterFactory());
		return options;
	}

	private sealed class TestDocument
	{
		public string? Title { get; set; }
		public int? Year { get; set; }
		public string? Author { get; set; }
	}

	// ── SearchRequest serialization ──

	[Fact]
	public void Serialize_SearchRequest_WithMatchAllQuery_ProducesCorrectJson()
	{
		var request = new SearchRequest
		{
			Query = QueryContainer.MatchAll(new MatchAllQuery { Boost = 1.0f }),
		};

		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("query", out var queryEl).Should().BeTrue();
		queryEl.TryGetProperty("match_all", out var matchAllEl).Should().BeTrue();
		matchAllEl.TryGetProperty("boost", out var boostEl).Should().BeTrue();
		boostEl.GetSingle().Should().Be(1.0f);
	}

	[Fact]
	public void Serialize_SearchRequest_IncludesBodyFields()
	{
		// Size and From are body fields (not query-string-only), so they appear in JSON body
		var request = new SearchRequest
		{
			Size = 10,
			From = 20,
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
		};

		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("size", out var sizeEl).Should().BeTrue();
		sizeEl.GetInt32().Should().Be(10);
		doc.RootElement.TryGetProperty("from", out var fromEl).Should().BeTrue();
		fromEl.GetInt32().Should().Be(20);
		doc.RootElement.TryGetProperty("query", out _).Should().BeTrue();
	}

	[Fact]
	public void Serialize_SearchRequest_WithProfileEnabled_IncludesProfileField()
	{
		var request = new SearchRequest
		{
			Profile = true,
			Query = QueryContainer.MatchAll(new MatchAllQuery()),
		};

		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("profile", out var profileEl).Should().BeTrue();
		profileEl.GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Serialize_SearchRequest_WithMinScore_IncludesMinScoreField()
	{
		var request = new SearchRequest
		{
			MinScore = 0.5f,
		};

		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("min_score", out var minScoreEl).Should().BeTrue();
		minScoreEl.GetSingle().Should().Be(0.5f);
	}

	// ── SearchResponse deserialization ──

	[Fact]
	public void Deserialize_SearchResponse_WithHits_ReturnsSourceDocuments()
	{
		var json = """
		{
			"took": 5,
			"timed_out": false,
			"_shards": {
				"total": 3,
				"successful": 3,
				"skipped": 0,
				"failed": 0
			},
			"hits": {
				"total": {
					"value": 2,
					"relation": "eq"
				},
				"max_score": 1.0,
				"hits": [
					{
						"_index": "books",
						"_id": "1",
						"_score": 1.0,
						"_source": {
							"title": "OpenSearch in Action",
							"year": 2024,
							"author": "Jane Doe"
						}
					},
					{
						"_index": "books",
						"_id": "2",
						"_score": 0.8,
						"_source": {
							"title": "Search Patterns",
							"year": 2023,
							"author": "John Smith"
						}
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Took.Should().Be(5);
		response.TimedOut.Should().BeFalse();
		response.Shards.Should().NotBeNull();
		response.Shards!.Total.Should().Be(3);
		response.Shards.Successful.Should().Be(3);
		response.Shards.Failed.Should().Be(0);

		response.Hits.Should().NotBeNull();
		response.Hits!.Hits.Should().HaveCount(2);

		var firstHit = response.Hits.Hits![0];
		firstHit.Index.Should().Be("books");
		firstHit.Id.Should().Be("1");
		firstHit.Source.Should().NotBeNull();
		firstHit.Source!.Title.Should().Be("OpenSearch in Action");
		firstHit.Source.Year.Should().Be(2024);
		firstHit.Source.Author.Should().Be("Jane Doe");

		var secondHit = response.Hits.Hits[1];
		secondHit.Id.Should().Be("2");
		secondHit.Source!.Title.Should().Be("Search Patterns");
	}

	[Fact]
	public void Deserialize_SearchResponse_WithAggregations_ParsesAggregationsDictionary()
	{
		var json = """
		{
			"took": 10,
			"timed_out": false,
			"_shards": {
				"total": 1,
				"successful": 1,
				"skipped": 0,
				"failed": 0
			},
			"hits": {
				"total": { "value": 100, "relation": "eq" },
				"hits": []
			},
			"aggregations": {
				"avg_year": {
					"meta": { "description": "average publication year" }
				}
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Aggregations.Should().NotBeNull();
		response.Aggregations.Should().ContainKey("avg_year");
		response.Aggregations!["avg_year"].Meta.Should().ContainKey("description");
	}

	[Fact]
	public void Deserialize_SearchResponse_WithScrollId_ParsesScrollId()
	{
		var json = """
		{
			"took": 2,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"_scroll_id": "DXF1ZXJ5QW5kRmV0Y2gBAAAAAAAAAD4WYm9laVYtZndUQlNsdDcwakFMNjU1QQ=="
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.ScrollId.Should().Be("DXF1ZXJ5QW5kRmV0Y2gBAAAAAAAAAD4WYm9laVYtZndUQlNsdDcwakFMNjU1QQ==");
	}

	[Fact]
	public void Deserialize_SearchResponse_WithTerminatedEarly_ParsesFlag()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"terminated_early": true,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] }
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.TerminatedEarly.Should().BeTrue();
	}

	[Fact]
	public void Deserialize_SearchResponse_WithHitMetadata_ParsesVersionAndSeqNo()
	{
		var json = """
		{
			"took": 1,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"hits": [
					{
						"_index": "test",
						"_id": "1",
						"_score": 1.0,
						"_version": 3,
						"_seq_no": 42,
						"_primary_term": 1,
						"_routing": "shard-1",
						"_source": { "title": "Test" }
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		var hit = response!.Hits!.Hits![0];
		hit.Version.Should().Be(3);
		hit.SeqNo.Should().Be(42);
		hit.PrimaryTerm.Should().Be(1);
		hit.Routing.Should().Be("shard-1");
	}

	// ── Round-trip tests ──

	[Fact]
	public void RoundTrip_SearchRequest_SerializeDeserialize_PreservesQuery()
	{
		var original = new SearchRequest
		{
			Query = QueryContainer.MatchAll(new MatchAllQuery { Boost = 1.5f }),
			MinScore = 0.7f,
			Profile = true,
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<SearchRequest>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Query.Should().NotBeNull();
		deserialized.Query!.Kind.Should().Be(QueryKind.MatchAll);
		deserialized.Query.Get<MatchAllQuery>().Boost.Should().Be(1.5f);
		deserialized.MinScore.Should().Be(0.7f);
		deserialized.Profile.Should().BeTrue();
	}

	[Fact]
	public void RoundTrip_SearchResponse_SerializeDeserialize_PreservesHits()
	{
		var original = new SearchResponse<TestDocument>
		{
			Took = 42,
			TimedOut = false,
			Shards = new ShardStatistics
			{
				Total = 5,
				Successful = 5,
				Failed = 0,
				Skipped = 0,
			},
			Hits = new HitsMetadata<TestDocument>
			{
				Hits =
				[
					new Hit<TestDocument>
					{
						Index = "my-index",
						Id = "doc-1",
						Source = new TestDocument
						{
							Title = "Round Trip",
							Year = 2025,
							Author = "Tester",
						},
					},
				],
			},
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Took.Should().Be(42);
		deserialized.TimedOut.Should().BeFalse();
		deserialized.Shards!.Total.Should().Be(5);
		deserialized.Hits!.Hits.Should().HaveCount(1);

		var hit = deserialized.Hits.Hits![0];
		hit.Index.Should().Be("my-index");
		hit.Id.Should().Be("doc-1");
		hit.Source!.Title.Should().Be("Round Trip");
		hit.Source.Year.Should().Be(2025);
		hit.Source.Author.Should().Be("Tester");
	}

	[Fact]
	public void Deserialize_SearchResponse_WithNumbersAsStrings_HandlesGracefully()
	{
		// OpenSearch can return numbers as strings with NumberHandling.AllowReadingFromString
		var json = """
		{
			"took": "7",
			"timed_out": false,
			"_shards": { "total": "1", "successful": "1", "skipped": "0", "failed": "0" },
			"hits": {
				"hits": [
					{
						"_index": "test",
						"_id": "1",
						"_source": { "title": "Stringy Numbers", "year": "2024" }
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Took.Should().Be(7);
		response.Shards!.Total.Should().Be(1);
		response.Hits!.Hits![0].Source!.Year.Should().Be(2024);
	}

	[Fact]
	public void Deserialize_SearchResponse_EmptyHits_ReturnsEmptyList()
	{
		var json = """
		{
			"took": 0,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": { "value": 0, "relation": "eq" },
				"hits": []
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDocument>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Hits!.Hits.Should().BeEmpty();
	}
}
