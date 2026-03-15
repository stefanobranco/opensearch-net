using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SuggestDictionaryTests
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

	// ── Term suggest ──

	[Fact]
	public void GetTerm_ReturnsTypedEntries()
	{
		var json = """
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"suggest": {
				"my_term": [
					{
						"text": "tset",
						"offset": 0,
						"length": 4,
						"options": [
							{ "text": "test", "score": 0.75, "freq": 10 },
							{ "text": "text", "score": 0.50, "freq": 3 }
						]
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var entries = response.Suggestions().GetTerm("my_term");

		entries.Should().NotBeNull();
		entries.Should().HaveCount(1);
		entries![0].Text.Should().Be("tset");
		entries[0].Offset.Should().Be(0);
		entries[0].Length.Should().Be(4);
		entries[0].Options.Should().HaveCount(2);
		entries[0].Options[0].Text.Should().Be("test");
		entries[0].Options[0].Score.Should().Be(0.75);
		entries[0].Options[0].Freq.Should().Be(10);
		entries[0].Options[1].Text.Should().Be("text");
	}

	// ── Phrase suggest ──

	[Fact]
	public void GetPhrase_ReturnsTypedEntries()
	{
		var json = """
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"suggest": {
				"phrase_suggest": [
					{
						"text": "noble prize",
						"offset": 0,
						"length": 11,
						"options": [
							{ "text": "nobel prize", "score": 0.95, "highlighted": "<em>nobel</em> prize" }
						]
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var entries = response.Suggestions().GetPhrase("phrase_suggest");

		entries.Should().NotBeNull();
		entries![0].Options[0].Text.Should().Be("nobel prize");
		entries[0].Options[0].Highlighted.Should().Be("<em>nobel</em> prize");
	}

	// ── Completion suggest ──

	[Fact]
	public void GetCompletion_ReturnsTypedEntriesWithSource()
	{
		var json = """
		{
			"took": 1, "timed_out": false,
			"_shards": { "total": 1, "successful": 1, "skipped": 0, "failed": 0 },
			"hits": { "hits": [] },
			"suggest": {
				"title_complete": [
					{
						"text": "op",
						"offset": 0,
						"length": 2,
						"options": [
							{
								"text": "OpenSearch Guide",
								"score": 1.0,
								"_index": "books",
								"_id": "1",
								"_source": { "title": "OpenSearch Guide" }
							}
						]
					}
				]
			}
		}
		""";

		var response = JsonSerializer.Deserialize<SearchResponse<TestDoc>>(json, JsonOptions)!;
		var entries = response.Suggestions().GetCompletion("title_complete");

		entries.Should().NotBeNull();
		entries![0].Options[0].Text.Should().Be("OpenSearch Guide");
		entries[0].Options[0].Source.Should().NotBeNull();
		entries[0].Options[0].Source!.Title.Should().Be("OpenSearch Guide");
		entries[0].Options[0].Index.Should().Be("books");
		entries[0].Options[0].Id.Should().Be("1");
	}

	// ── Missing suggest ──

	[Fact]
	public void GetTerm_MissingSuggester_ReturnsNull()
	{
		var response = new SearchResponse<TestDoc>();
		response.Suggestions().GetTerm("nonexistent").Should().BeNull();
	}

	[Fact]
	public void GetTerm_NullSuggest_ReturnsNull()
	{
		var dict = new SuggestDictionary<TestDoc>(null);
		dict.GetTerm("anything").Should().BeNull();
	}
}
