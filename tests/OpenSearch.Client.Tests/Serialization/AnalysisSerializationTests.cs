using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Wire-format fixtures for the analysis chain — the <see cref="CharFilter"/>, <see cref="TokenFilter"/>,
/// and <see cref="Tokenizer"/> wrappers (each <c>oneOf[string built-in name, definition]</c>) and a full
/// <see cref="IndexSettingsAnalysis"/>. These types were previously untyped (<c>JsonElement</c>); the fixtures
/// prove both wire forms — a bare string name and an internally-tagged definition object — round-trip through
/// the production serializer.
/// </summary>
public class AnalysisSerializationTests : SerializationTestBase
{
	[Fact]
	public void CharFilter_builtin_name_serializes_as_bare_string()
	{
		CharFilter filter = "html_strip";

		var json = Serialize(filter);
		json.Should().Be("\"html_strip\"");

		var back = Deserialize<CharFilter>(json);
		back!.Name.Should().Be("html_strip");
		back.Definition.Should().BeNull();
	}

	[Fact]
	public void CharFilter_definition_serializes_internally_tagged_and_round_trips()
	{
		CharFilter filter = CharFilterDefinition.MappingCharFilter(new MappingCharFilter
		{
			Mappings = ["٠ => 0", "١ => 1"],
		});

		var root = AssertRoundTrips(filter);
		root.ValueKind.Should().Be(JsonValueKind.Object);
		root.GetProperty("type").GetString().Should().Be("mapping");
		root.GetProperty("mappings").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void TokenFilter_builtin_name_round_trips()
	{
		TokenFilter filter = "lowercase";
		var root = AssertRoundTrips(filter);
		root.ValueKind.Should().Be(JsonValueKind.String);
		root.GetString().Should().Be("lowercase");
	}

	[Fact]
	public void TokenFilter_definition_serializes_internally_tagged_and_round_trips()
	{
		TokenFilter filter = TokenFilterDefinition.StopTokenFilter(new StopTokenFilter
		{
			Stopwords = ["and", "the"],
			IgnoreCase = true,
		});

		var root = AssertRoundTrips(filter);
		root.GetProperty("type").GetString().Should().Be("stop");
		root.GetProperty("stopwords").GetArrayLength().Should().Be(2);
		root.GetProperty("ignore_case").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Tokenizer_builtin_name_round_trips()
	{
		Tokenizer tokenizer = "standard";
		var root = AssertRoundTrips(tokenizer);
		root.GetString().Should().Be("standard");
	}

	[Fact]
	public void Tokenizer_definition_serializes_internally_tagged_and_round_trips()
	{
		Tokenizer tokenizer = TokenizerDefinition.NGramTokenizer(new NGramTokenizer
		{
			MinGram = 2,
			MaxGram = 3,
		});

		var root = AssertRoundTrips(tokenizer);
		root.GetProperty("type").GetString().Should().Be("ngram");
		root.GetProperty("min_gram").GetInt32().Should().Be(2);
		root.GetProperty("max_gram").GetInt32().Should().Be(3);
	}

	[Fact]
	public void Analysis_reads_back_definition_from_object_and_name_from_string()
	{
		// Read path: an object dispatches to the definition union; a string is the built-in name.
		var def = Deserialize<Tokenizer>("""{"type":"keyword","buffer_size":256}""");
		def!.Definition.Should().NotBeNull();
		def.Definition!.Kind.Should().Be(TokenizerDefinitionKind.KeywordTokenizer);
		def.Name.Should().BeNull();

		var name = Deserialize<Tokenizer>("\"whitespace\"");
		name!.Name.Should().Be("whitespace");
		name.Definition.Should().BeNull();
	}

	[Fact]
	public void IndexSettingsAnalysis_with_named_components_serializes_and_round_trips()
	{
		// The canonical custom-analysis block: named char_filter/filter/tokenizer definitions plus a
		// custom analyzer that references them by name.
		var analysis = new IndexSettingsAnalysis
		{
			CharFilter = new Dictionary<string, CharFilter>
			{
				["my_mapping"] = CharFilterDefinition.MappingCharFilter(new MappingCharFilter { Mappings = ["a => b"] }),
			},
			Filter = new Dictionary<string, TokenFilter>
			{
				["my_stop"] = TokenFilterDefinition.StopTokenFilter(new StopTokenFilter { Stopwords = ["the"] }),
			},
			Tokenizer = new Dictionary<string, Tokenizer>
			{
				["my_ngram"] = TokenizerDefinition.NGramTokenizer(new NGramTokenizer { MinGram = 1, MaxGram = 2 }),
			},
			Analyzer = new Dictionary<string, Analyzer>
			{
				["my_analyzer"] = Analyzer.CustomAnalyzer(new CustomAnalyzer
				{
					Tokenizer = "my_ngram",
					CharFilter = ["my_mapping"],
					Filter = ["my_stop", "lowercase"],
				}),
			},
		};

		var root = AssertRoundTrips(analysis);

		root.GetProperty("char_filter").GetProperty("my_mapping").GetProperty("type").GetString().Should().Be("mapping");
		root.GetProperty("filter").GetProperty("my_stop").GetProperty("type").GetString().Should().Be("stop");
		root.GetProperty("tokenizer").GetProperty("my_ngram").GetProperty("type").GetString().Should().Be("ngram");

		var analyzer = root.GetProperty("analyzer").GetProperty("my_analyzer");
		analyzer.GetProperty("type").GetString().Should().Be("custom");
		analyzer.GetProperty("tokenizer").GetString().Should().Be("my_ngram");
		analyzer.GetProperty("filter")[1].GetString().Should().Be("lowercase");
	}

	[Fact]
	public void IndexSettingsAnalysis_with_builtin_tokenizer_name_serializes_bare_string()
	{
		// A named entry can also be a built-in reference (bare string), not just a definition.
		var analysis = new IndexSettingsAnalysis
		{
			Tokenizer = new Dictionary<string, Tokenizer> { ["passthrough"] = "keyword" },
		};

		var root = AssertRoundTrips(analysis);
		root.GetProperty("tokenizer").GetProperty("passthrough").ValueKind.Should().Be(JsonValueKind.String);
		root.GetProperty("tokenizer").GetProperty("passthrough").GetString().Should().Be("keyword");
	}
}
