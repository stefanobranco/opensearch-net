using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class BulkSerializationTests
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

	// ── BulkResponse deserialization ──

	[Fact]
	public void Deserialize_BulkResponse_AllSuccessful()
	{
		var json = """
		{
			"took": 30,
			"errors": false,
			"items": [
				{
					"index": {
						"_index": "test-index",
						"_id": "1",
						"_version": 1,
						"result": "created",
						"_shards": { "total": 2, "successful": 1, "failed": 0 },
						"_seq_no": 0,
						"_primary_term": 1,
						"status": 201
					}
				},
				{
					"index": {
						"_index": "test-index",
						"_id": "2",
						"_version": 1,
						"result": "created",
						"_shards": { "total": 2, "successful": 1, "failed": 0 },
						"_seq_no": 1,
						"_primary_term": 1,
						"status": 201
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Took.Should().Be(30);
		response.Errors.Should().BeFalse();
		response.Items.Should().HaveCount(2);

		var firstItem = response.Items![0];
		firstItem.Index.Should().NotBeNull();
		firstItem.Index!.Index.Should().Be("test-index");
		firstItem.Index.Id.Should().Be("1");
		firstItem.Index.Version.Should().Be(1);
		firstItem.Index.Result.Should().Be("created");
		firstItem.Index.SeqNo.Should().Be(0);
		firstItem.Index.PrimaryTerm.Should().Be(1);
		firstItem.Index.Status.Should().Be(201);
	}

	[Fact]
	public void Deserialize_BulkResponse_WithErrors()
	{
		var json = """
		{
			"took": 50,
			"errors": true,
			"items": [
				{
					"index": {
						"_index": "test-index",
						"_id": "1",
						"_version": 1,
						"result": "created",
						"status": 201,
						"_seq_no": 0,
						"_primary_term": 1
					}
				},
				{
					"index": {
						"_index": "test-index",
						"_id": "2",
						"status": 409,
						"error": {
							"type": "version_conflict_engine_exception",
							"reason": "[2]: version conflict, document already exists (current version [1])"
						}
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Errors.Should().BeTrue();
		response.Items.Should().HaveCount(2);

		var successItem = response.Items![0];
		successItem.Index!.Status.Should().Be(201);
		successItem.Index.Result.Should().Be("created");

		var errorItem = response.Items[1];
		errorItem.Index!.Status.Should().Be(409);
		errorItem.Index.Error.Should().NotBeNull();
		errorItem.Index.Error!.Value.GetProperty("type").GetString()
			.Should().Be("version_conflict_engine_exception");
	}

	[Fact]
	public void Deserialize_BulkResponse_MixedOperations()
	{
		var json = """
		{
			"took": 15,
			"errors": false,
			"items": [
				{
					"index": {
						"_index": "test",
						"_id": "1",
						"_version": 1,
						"result": "created",
						"status": 201,
						"_seq_no": 0,
						"_primary_term": 1
					}
				},
				{
					"create": {
						"_index": "test",
						"_id": "2",
						"_version": 1,
						"result": "created",
						"status": 201,
						"_seq_no": 1,
						"_primary_term": 1
					}
				},
				{
					"update": {
						"_index": "test",
						"_id": "3",
						"_version": 2,
						"result": "updated",
						"status": 200,
						"_seq_no": 2,
						"_primary_term": 1
					}
				},
				{
					"delete": {
						"_index": "test",
						"_id": "4",
						"_version": 3,
						"result": "deleted",
						"status": 200,
						"_seq_no": 3,
						"_primary_term": 1
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Errors.Should().BeFalse();
		response.Items.Should().HaveCount(4);

		// Index operation
		response.Items![0].Index.Should().NotBeNull();
		response.Items[0].Index!.Result.Should().Be("created");

		// Create operation
		response.Items[1].Create.Should().NotBeNull();
		response.Items[1].Create!.Id.Should().Be("2");
		response.Items[1].Create!.Result.Should().Be("created");

		// Update operation
		response.Items[2].Update.Should().NotBeNull();
		response.Items[2].Update!.Id.Should().Be("3");
		response.Items[2].Update!.Result.Should().Be("updated");
		response.Items[2].Update!.Version.Should().Be(2);

		// Delete operation
		response.Items[3].Delete.Should().NotBeNull();
		response.Items[3].Delete!.Id.Should().Be("4");
		response.Items[3].Delete!.Result.Should().Be("deleted");
	}

	[Fact]
	public void RoundTrip_BulkResponse_SerializeDeserialize_PreservesFields()
	{
		var original = new BulkResponse
		{
			Took = 25,
			Errors = false,
			Items =
			[
				new BulkResponseItem
				{
					Index = new BulkResponseItemResult
					{
						Index = "my-index",
						Id = "doc-1",
						Version = 1,
						Result = "created",
						SeqNo = 0,
						PrimaryTerm = 1,
						Status = 201,
					},
				},
			],
		};

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.Took.Should().Be(25);
		deserialized.Errors.Should().BeFalse();
		deserialized.Items.Should().HaveCount(1);
		deserialized.Items![0].Index!.Index.Should().Be("my-index");
		deserialized.Items[0].Index!.Id.Should().Be("doc-1");
		deserialized.Items[0].Index!.Version.Should().Be(1);
		deserialized.Items[0].Index!.Status.Should().Be(201);
	}

	[Fact]
	public void Deserialize_BulkResponse_EmptyItems()
	{
		var json = """
		{
			"took": 0,
			"errors": false,
			"items": []
		}
		""";

		var response = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Items.Should().BeEmpty();
	}

	[Fact]
	public void Deserialize_BulkResponse_WithNumbersAsStrings()
	{
		var json = """
		{
			"took": "12",
			"errors": false,
			"items": [
				{
					"index": {
						"_index": "test",
						"_id": "1",
						"_version": "1",
						"result": "created",
						"_seq_no": "0",
						"_primary_term": "1",
						"status": 201
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<BulkResponse>(json, JsonOptions);

		response.Should().NotBeNull();
		response!.Took.Should().Be(12);
		response.Items![0].Index!.Version.Should().Be(1);
		response.Items[0].Index!.SeqNo.Should().Be(0);
		response.Items[0].Index!.PrimaryTerm.Should().Be(1);
	}
}
