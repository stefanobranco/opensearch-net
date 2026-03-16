using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class MgetResponseExtensionsTests
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
	public void GetDocs_DeserializesFoundDocuments()
	{
		var json = """
		{
			"docs": [
				{
					"_index": "books",
					"_id": "1",
					"_version": 3,
					"_seq_no": 10,
					"_primary_term": 1,
					"found": true,
					"_source": { "title": "OpenSearch Guide" }
				},
				{
					"_index": "books",
					"_id": "2",
					"_version": 1,
					"found": true,
					"_source": { "title": "Search Patterns" }
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MgetResponse>(json, JsonOptions)!;
		var docs = response.GetDocs<TestDoc>();

		docs.Should().HaveCount(2);

		docs[0].Found.Should().BeTrue();
		docs[0].Index.Should().Be("books");
		docs[0].Id.Should().Be("1");
		docs[0].Version.Should().Be(3);
		docs[0].SeqNo.Should().Be(10);
		docs[0].PrimaryTerm.Should().Be(1);
		docs[0].Source.Should().NotBeNull();
		docs[0].Source!.Title.Should().Be("OpenSearch Guide");

		docs[1].Found.Should().BeTrue();
		docs[1].Source!.Title.Should().Be("Search Patterns");
	}

	[Fact]
	public void GetDocs_HandlesNotFoundDocuments()
	{
		var json = """
		{
			"docs": [
				{
					"_index": "books",
					"_id": "1",
					"found": true,
					"_source": { "title": "Exists" }
				},
				{
					"_index": "books",
					"_id": "999",
					"found": false
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize<MgetResponse>(json, JsonOptions)!;
		var docs = response.GetDocs<TestDoc>();

		docs.Should().HaveCount(2);

		docs[0].Found.Should().BeTrue();
		docs[0].Source!.Title.Should().Be("Exists");

		docs[1].Found.Should().BeFalse();
		docs[1].Id.Should().Be("999");
		docs[1].Source.Should().BeNull();
	}

	[Fact]
	public void GetDocs_EmptyDocs_ReturnsEmptyList()
	{
		var json = """{ "docs": [] }""";
		var response = JsonSerializer.Deserialize<MgetResponse>(json, JsonOptions)!;
		response.GetDocs<TestDoc>().Should().BeEmpty();
	}

	[Fact]
	public void GetDocs_NullDocs_ReturnsEmptyList()
	{
		var response = new MgetResponse();
		response.GetDocs<TestDoc>().Should().BeEmpty();
	}
}
