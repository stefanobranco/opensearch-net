using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using OpenSearch.Client.Indices;
using Xunit;

namespace OpenSearch.Client.Tests;

public class IndexSerializationTests
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

	private sealed class Product
	{
		public string? Name { get; set; }
		public decimal? Price { get; set; }
		public string? Category { get; set; }
	}

	// ── IndexResponse deserialization ──

	[Fact]
	public void Deserialize_IndexResponse_SuccessfulCreation()
	{
		var json = """
		{
			"_index": "products",
			"_id": "abc123",
			"_version": 1,
			"result": "created",
			"_shards": {
				"total": 2,
				"successful": 1,
				"failed": 0
			},
			"_seq_no": 0,
			"_primary_term": 1
		}
		""";

		var response = JsonSerializer.Deserialize<IndexResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Index.Should().Be("products");
		response.Id.Should().Be("abc123");
		response.Version.Should().Be(1);
		response.SeqNo.Should().Be(0);
		response.PrimaryTerm.Should().Be(1);
		response.Shards.Should().NotBeNull();
		response.Shards!.Total.Should().Be(2);
		response.Shards.Successful.Should().Be(1);
		response.Shards.Failed.Should().Be(0);
	}

	[Fact]
	public void Deserialize_IndexResponse_UpdatedDocument()
	{
		var json = """
		{
			"_index": "products",
			"_id": "abc123",
			"_version": 2,
			"result": "updated",
			"_shards": {
				"total": 2,
				"successful": 2,
				"failed": 0
			},
			"_seq_no": 5,
			"_primary_term": 1,
			"forced_refresh": true
		}
		""";

		var response = JsonSerializer.Deserialize<IndexResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Version.Should().Be(2);
		response.SeqNo.Should().Be(5);
		response.ForcedRefresh.Should().BeTrue();
	}

	[Fact]
	public void RoundTrip_IndexResponse_SerializeDeserialize_PreservesFields()
	{
		var original = new IndexResponse
		{
			Index = "test-index",
			Id = "doc-42",
			Version = 3,
			SeqNo = 15,
			PrimaryTerm = 2,
			Shards = new ShardStatistics
			{
				Total = 2,
				Successful = 2,
				Failed = 0,
			},
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<IndexResponse>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Index.Should().Be("test-index");
		deserialized.Id.Should().Be("doc-42");
		deserialized.Version.Should().Be(3);
		deserialized.SeqNo.Should().Be(15);
		deserialized.PrimaryTerm.Should().Be(2);
		deserialized.Shards!.Total.Should().Be(2);
		deserialized.Shards.Successful.Should().Be(2);
	}

	[Fact]
	public void Serialize_IndexResponse_UsesJsonPropertyNames()
	{
		var response = new IndexResponse
		{
			Index = "products",
			Id = "1",
			Version = 1,
			SeqNo = 0,
			PrimaryTerm = 1,
		};

		var json = JsonSerializer.Serialize(response, JsonOptions);

		// Fields with [JsonPropertyName] should use the explicit names
		json.Should().Contain("\"_index\"");
		json.Should().Contain("\"_id\"");
		json.Should().Contain("\"_version\"");
		json.Should().Contain("\"_seq_no\"");
		json.Should().Contain("\"_primary_term\"");
	}

	[Fact]
	public void Deserialize_IndexResponse_WithType_ParsesLegacyTypeField()
	{
		var json = """
		{
			"_index": "products",
			"_type": "_doc",
			"_id": "1",
			"_version": 1,
			"result": "created",
			"_shards": { "total": 2, "successful": 1, "failed": 0 },
			"_seq_no": 0,
			"_primary_term": 1
		}
		""";

		var response = JsonSerializer.Deserialize<IndexResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Type.Should().Be("_doc");
	}

	// ── GetResponse deserialization ──

	[Fact]
	public void Deserialize_GetResponse_FoundDocument()
	{
		var json = """
		{
			"_index": "products",
			"_id": "abc123",
			"_version": 1,
			"_seq_no": 0,
			"_primary_term": 1,
			"found": true,
			"_source": {
				"name": "Widget",
				"price": 29.99,
				"category": "Tools"
			}
		}
		""";

		var response = JsonSerializer.Deserialize<GetResponse<Product>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Found.Should().BeTrue();
		response.Index.Should().Be("products");
		response.Id.Should().Be("abc123");
		response.Version.Should().Be(1);
		response.SeqNo.Should().Be(0);
		response.PrimaryTerm.Should().Be(1);
		response.Source.Should().NotBeNull();
		response.Source!.Name.Should().Be("Widget");
		response.Source.Price.Should().Be(29.99m);
		response.Source.Category.Should().Be("Tools");
	}

	[Fact]
	public void Deserialize_GetResponse_NotFound()
	{
		var json = """
		{
			"_index": "products",
			"_id": "nonexistent",
			"found": false
		}
		""";

		var response = JsonSerializer.Deserialize<GetResponse<Product>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Found.Should().BeFalse();
		response.Id.Should().Be("nonexistent");
		response.Source.Should().BeNull();
	}

	[Fact]
	public void Deserialize_GetResponse_WithRouting()
	{
		var json = """
		{
			"_index": "products",
			"_id": "1",
			"_version": 1,
			"_routing": "user-123",
			"_seq_no": 5,
			"_primary_term": 1,
			"found": true,
			"_source": { "name": "Gadget" }
		}
		""";

		var response = JsonSerializer.Deserialize<GetResponse<Product>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Routing.Should().Be("user-123");
	}

	[Fact]
	public void RoundTrip_GetResponse_SerializeDeserialize_PreservesAllFields()
	{
		var original = new GetResponse<Product>
		{
			Index = "products",
			Id = "round-trip-1",
			Version = 5,
			SeqNo = 22,
			PrimaryTerm = 3,
			Found = true,
			Routing = "custom-route",
			Source = new Product
			{
				Name = "Round Trip Product",
				Price = 49.99m,
				Category = "Testing",
			},
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<GetResponse<Product>>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Index.Should().Be("products");
		deserialized.Id.Should().Be("round-trip-1");
		deserialized.Version.Should().Be(5);
		deserialized.SeqNo.Should().Be(22);
		deserialized.PrimaryTerm.Should().Be(3);
		deserialized.Found.Should().BeTrue();
		deserialized.Routing.Should().Be("custom-route");
		deserialized.Source!.Name.Should().Be("Round Trip Product");
		deserialized.Source.Price.Should().Be(49.99m);
		deserialized.Source.Category.Should().Be("Testing");
	}

	[Fact]
	public void Deserialize_GetResponse_WithNumbersAsStrings()
	{
		var json = """
		{
			"_index": "products",
			"_id": "1",
			"_version": "3",
			"_seq_no": "10",
			"_primary_term": "2",
			"found": true,
			"_source": { "name": "Item", "price": "19.99" }
		}
		""";

		var response = JsonSerializer.Deserialize<GetResponse<Product>>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Version.Should().Be(3);
		response.SeqNo.Should().Be(10);
		response.PrimaryTerm.Should().Be(2);
	}
}
