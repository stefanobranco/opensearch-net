using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SuggesterDescriptorTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	[Fact]
	public void Completion_AddsNamedCompletionSuggester()
	{
		var descriptor = new SuggesterDescriptor();
		descriptor.Completion("title_suggest", c => c
			.Field("title.suggest")
			.Size(5)
			.SkipDuplicates(true));

		Suggester suggester = descriptor;
		var json = JsonSerializer.Serialize(suggester, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("title_suggest", out var suggestEl).Should().BeTrue();
		suggestEl.TryGetProperty("completion", out var completionEl).Should().BeTrue();
		completionEl.GetProperty("field").GetString().Should().Be("title.suggest");
		completionEl.GetProperty("size").GetInt32().Should().Be(5);
		completionEl.GetProperty("skip_duplicates").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Completion_WithPrefix_IncludesPrefixField()
	{
		var descriptor = new SuggesterDescriptor();
		descriptor.Completion("title_suggest", c => c
			.Field("title.suggest")
			.Size(10),
			prefix: "open");

		Suggester suggester = descriptor;
		var json = JsonSerializer.Serialize(suggester, JsonOptions);
		var doc = JsonDocument.Parse(json);

		var suggestEl = doc.RootElement.GetProperty("title_suggest");
		suggestEl.GetProperty("prefix").GetString().Should().Be("open");
		suggestEl.TryGetProperty("completion", out _).Should().BeTrue();
	}

	[Fact]
	public void Term_AddsNamedTermSuggester()
	{
		var descriptor = new SuggesterDescriptor();
		descriptor.Term("spell_check", t => t
			.Field("content")
			.Size(3)
			.SuggestMode(SuggestMode.Always));

		Suggester suggester = descriptor;
		var json = JsonSerializer.Serialize(suggester, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("spell_check", out var suggestEl).Should().BeTrue();
		suggestEl.TryGetProperty("term", out var termEl).Should().BeTrue();
		termEl.GetProperty("field").GetString().Should().Be("content");
		termEl.GetProperty("size").GetInt32().Should().Be(3);
	}

	[Fact]
	public void Phrase_AddsNamedPhraseSuggester()
	{
		var descriptor = new SuggesterDescriptor();
		descriptor.Phrase("phrase_suggest", p => p
			.Field("content")
			.GramSize(2)
			.Confidence(1.0));

		Suggester suggester = descriptor;
		var json = JsonSerializer.Serialize(suggester, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("phrase_suggest", out var suggestEl).Should().BeTrue();
		suggestEl.TryGetProperty("phrase", out var phraseEl).Should().BeTrue();
		phraseEl.GetProperty("field").GetString().Should().Be("content");
		phraseEl.GetProperty("gram_size").GetInt32().Should().Be(2);
	}

	[Fact]
	public void MultipleNamedSuggesters_AllPresent()
	{
		var descriptor = new SuggesterDescriptor();
		descriptor
			.Text("opensearch")
			.Completion("autocomplete", c => c.Field("suggest").Size(5))
			.Term("did_you_mean", t => t.Field("content").Size(3));

		Suggester suggester = descriptor;
		var json = JsonSerializer.Serialize(suggester, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.GetProperty("text").GetString().Should().Be("opensearch");
		doc.RootElement.TryGetProperty("autocomplete", out _).Should().BeTrue();
		doc.RootElement.TryGetProperty("did_you_mean", out _).Should().BeTrue();
	}

	[Fact]
	public void Completion_ViaSearchRequestDescriptor_SerializesCorrectly()
	{
		var searchDesc = new SearchRequestDescriptor<object>();
		searchDesc.Suggest(s => s
			.Completion("title_suggest", c => c
				.Field("title.suggest")
				.Size(5),
				prefix: "test"));

		SearchRequest request = searchDesc;
		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("suggest", out var suggestEl).Should().BeTrue();
		suggestEl.GetProperty("title_suggest").TryGetProperty("completion", out _).Should().BeTrue();
		suggestEl.GetProperty("title_suggest").GetProperty("prefix").GetString().Should().Be("test");
	}
}
