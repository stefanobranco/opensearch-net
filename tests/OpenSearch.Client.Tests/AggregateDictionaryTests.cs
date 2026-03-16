using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class AggregateDictionaryTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	private sealed class TestDoc
	{
		public string? Status { get; set; }
		public double? Price { get; set; }
	}

	private SearchResponse<TestDoc> Deserialize(string json)
		=> JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;

	// ── Metric accessors ──

	[Fact]
	public void Average_ReturnsMetricValue()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": { "avg_price": { "value": 29.95 } }
		}
		""");

		response.Aggs().Average("avg_price").Should().Be(29.95);
	}

	[Fact]
	public void Sum_ReturnsMetricValue()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": { "total_sales": { "value": 15000.50 } }
		}
		""");

		response.Aggs().Sum("total_sales").Should().Be(15000.50);
	}

	[Fact]
	public void Min_ReturnsMetricValue()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": { "min_price": { "value": 1.50 } }
		}
		""");

		response.Aggs().Min("min_price").Should().Be(1.50);
	}

	[Fact]
	public void Max_ReturnsMetricValue()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": { "max_price": { "value": 999.99 } }
		}
		""");

		response.Aggs().Max("max_price").Should().Be(999.99);
	}

	[Fact]
	public void Cardinality_ReturnsLongValue()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": { "unique_users": { "value": 42 } }
		}
		""");

		response.Aggs().Cardinality("unique_users").Should().Be(42);
	}

	[Fact]
	public void Stats_ReturnsAllFields()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"price_stats": { "count": 100, "min": 1.0, "max": 50.0, "avg": 25.0, "sum": 2500.0 }
			}
		}
		""");

		var stats = response.Aggs().Stats("price_stats");
		stats.Should().NotBeNull();
		stats!.Count.Should().Be(100);
		stats.Min.Should().Be(1.0);
		stats.Max.Should().Be(50.0);
		stats.Avg.Should().Be(25.0);
		stats.Sum.Should().Be(2500.0);
	}

	[Fact]
	public void NonexistentAggregation_ReturnsNull()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {}
		}
		""");

		response.Aggs().Average("missing").Should().BeNull();
		response.Aggs().Terms("missing").Should().BeNull();
	}

	// ── Bucket accessors ──

	[Fact]
	public void Terms_ReturnsBuckets()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"by_status": {
					"doc_count_error_upper_bound": 0,
					"sum_other_doc_count": 0,
					"buckets": [
						{ "key": "active", "doc_count": 42 },
						{ "key": "inactive", "doc_count": 18 }
					]
				}
			}
		}
		""");

		var buckets = response.Aggs().Terms("by_status");
		buckets.Should().NotBeNull();
		buckets.Should().HaveCount(2);
		buckets![0].Key.Should().Be("active");
		buckets[0].DocCount.Should().Be(42);
		buckets[1].Key.Should().Be("inactive");
		buckets[1].DocCount.Should().Be(18);
	}

	[Fact]
	public void Terms_WithSubAggregations_ChainsCorrectly()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"by_status": {
					"buckets": [
						{
							"key": "active",
							"doc_count": 42,
							"avg_price": { "value": 9.99 },
							"max_price": { "value": 49.99 }
						}
					]
				}
			}
		}
		""");

		var buckets = response.Aggs().Terms("by_status")!;
		buckets[0].Aggregations.Should().NotBeNull();
		// Sub-agg avg_price has "value": 9.99
		buckets[0].Aggregations!.Average("avg_price").Should().Be(9.99);
		// Sub-agg max_price has "value": 49.99
		buckets[0].Aggregations!.Max("max_price").Should().Be(49.99);
	}

	[Fact]
	public void DateHistogram_ReturnsBuckets()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"by_month": {
					"buckets": [
						{ "key": 1704067200000, "key_as_string": "2024-01-01T00:00:00.000Z", "doc_count": 10 },
						{ "key": 1706745600000, "key_as_string": "2024-02-01T00:00:00.000Z", "doc_count": 15 }
					]
				}
			}
		}
		""");

		var buckets = response.Aggs().DateHistogram("by_month");
		buckets.Should().HaveCount(2);
		buckets![0].Key.Should().Be(1704067200000);
		buckets[0].KeyAsString.Should().Be("2024-01-01T00:00:00.000Z");
		buckets[0].DocCount.Should().Be(10);
	}

	[Fact]
	public void Histogram_ReturnsBuckets()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"price_ranges": {
					"buckets": [
						{ "key": 0.0, "doc_count": 5 },
						{ "key": 50.0, "doc_count": 12 },
						{ "key": 100.0, "doc_count": 3 }
					]
				}
			}
		}
		""");

		var buckets = response.Aggs().Histogram("price_ranges");
		buckets.Should().HaveCount(3);
		buckets![0].Key.Should().Be(0.0);
		buckets[1].Key.Should().Be(50.0);
		buckets[1].DocCount.Should().Be(12);
	}

	[Fact]
	public void Range_ReturnsBuckets()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"price_range": {
					"buckets": [
						{ "key": "cheap", "from": 0, "to": 10, "doc_count": 5 },
						{ "key": "expensive", "from": 10, "to": 100, "doc_count": 15 }
					]
				}
			}
		}
		""");

		var buckets = response.Aggs().Range("price_range");
		buckets.Should().HaveCount(2);
		buckets![0].Key.Should().Be("cheap");
		buckets[0].From.Should().Be(0);
		buckets[0].To.Should().Be(10);
		buckets[0].DocCount.Should().Be(5);
	}

	// ── Typed keys ──

	[Fact]
	public void TypedKeys_AreStripped()
	{
		var response = Deserialize("""
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"aggregations": {
				"sterms#by_status": {
					"buckets": [
						{ "key": "active", "doc_count": 42 }
					]
				},
				"avg#avg_price": { "value": 9.99 }
			}
		}
		""");

		var aggs = response.Aggs();
		aggs.Terms("by_status").Should().HaveCount(1);
		aggs.Average("avg_price").Should().Be(9.99);
	}

	// ── Null/empty aggregations ──

	[Fact]
	public void NullAggregations_ReturnsEmptyDictionary()
	{
		var response = new SearchResponse<TestDoc>();
		var aggs = response.Aggs();
		aggs.Average("anything").Should().BeNull();
	}
}
