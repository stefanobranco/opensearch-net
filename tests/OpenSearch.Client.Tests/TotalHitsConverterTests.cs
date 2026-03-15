using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class TotalHitsConverterTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	[Fact]
	public void Deserialize_ObjectForm_ReadsBothFields()
	{
		var json = """{ "value": 42, "relation": "gte" }""";
		var total = JsonSerializer.Deserialize<TotalHits>(json, JsonOptions);

		total.Should().NotBeNull();
		total!.Value.Should().Be(42);
		total.Relation.Should().Be("gte");
	}

	[Fact]
	public void Deserialize_ObjectForm_DefaultsRelationToEq()
	{
		var json = """{ "value": 100, "relation": "eq" }""";
		var total = JsonSerializer.Deserialize<TotalHits>(json, JsonOptions);

		total!.Value.Should().Be(100);
		total.Relation.Should().Be("eq");
	}

	[Fact]
	public void Deserialize_BareInteger_ReturnsTotalHitsWithEqRelation()
	{
		var json = "500";
		var total = JsonSerializer.Deserialize<TotalHits>(json, JsonOptions);

		total.Should().NotBeNull();
		total!.Value.Should().Be(500);
		total.Relation.Should().Be("eq");
	}

	[Fact]
	public void Deserialize_Null_ReturnsNull()
	{
		var json = "null";
		var total = JsonSerializer.Deserialize<TotalHits>(json, JsonOptions);
		total.Should().BeNull();
	}

	[Fact]
	public void Serialize_WritesObjectForm()
	{
		var total = new TotalHits { Value = 42, Relation = "gte" };
		var json = JsonSerializer.Serialize(total, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.GetProperty("value").GetInt64().Should().Be(42);
		doc.RootElement.GetProperty("relation").GetString().Should().Be("gte");
	}

	[Fact]
	public void RoundTrip_ObjectForm_PreservesData()
	{
		var original = new TotalHits { Value = 1000, Relation = "gte" };
		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<TotalHits>(json, JsonOptions);

		deserialized!.Value.Should().Be(1000);
		deserialized.Relation.Should().Be("gte");
	}

	[Fact]
	public void ImplicitConversionToLong_ReturnsValue()
	{
		var total = new TotalHits { Value = 42 };
		long count = total;
		count.Should().Be(42);
	}

	[Fact]
	public void Deserialize_InHitsMetadata_ObjectForm_ParsesTotalHits()
	{
		var json = """
		{
			"took": 5,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": { "value": 200, "relation": "gte" },
				"hits": []
			}
		}
		""";

		var response = JsonSerializer.Deserialize<OpenSearch.Client.Core.SearchResponse<object>>(json, JsonOptions);
		response!.Hits!.Total.Should().NotBeNull();
		response.Hits.Total!.Value.Should().Be(200);
		response.Hits.Total.Relation.Should().Be("gte");
	}

	[Fact]
	public void Deserialize_InHitsMetadata_BareInteger_ParsesTotalHits()
	{
		var json = """
		{
			"took": 5,
			"timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": {
				"total": 42,
				"hits": []
			}
		}
		""";

		var response = JsonSerializer.Deserialize<OpenSearch.Client.Core.SearchResponse<object>>(json, JsonOptions);
		response!.Hits!.Total.Should().NotBeNull();
		response.Hits.Total!.Value.Should().Be(42);
		response.Hits.Total.Relation.Should().Be("eq");
	}
}
