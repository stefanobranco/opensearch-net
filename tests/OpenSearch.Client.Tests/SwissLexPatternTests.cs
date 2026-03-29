using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Tests that exercise the exact query patterns used in SwissLex production code.
/// Each test mirrors a real query builder from the SwissLex retrieval layer.
/// </summary>
public class SwissLexPatternTests
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

	// ── Simplified SwissLex document model ──

	private sealed class ElasticAsset
	{
		[JsonPropertyName("id")]
		public Guid Id { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("title_autocomplete")]
		public string? TitleAutocomplete { get; set; }

		[JsonPropertyName("search_visibility")]
		public short SearchVisibility { get; set; }

		[JsonPropertyName("meta")]
		public Meta? Meta { get; set; }

		[JsonPropertyName("type_information")]
		public AssetTypeInfo? AssetTypeInfo { get; set; }

		[JsonPropertyName("content")]
		public ContentObject? ContentObject { get; set; }

		[JsonPropertyName("publication_meta")]
		public PublicationMeta? PublicationMeta { get; set; }

		[JsonPropertyName("law_references")]
		public List<LawReference>? LawReferences { get; set; }

		[JsonPropertyName("navigation_information")]
		public NavigationInfo? NavigationInfo { get; set; }

		[JsonPropertyName("access_rights")]
		public List<string>? Rights { get; set; }
	}

	private sealed class Meta
	{
		[JsonPropertyName("display_type")]
		public string? DisplayType { get; set; }

		[JsonPropertyName("language")]
		public string? Language { get; set; }

		[JsonPropertyName("publication_date")]
		public DateTime? PublicationDate { get; set; }

		[JsonPropertyName("collection_id")]
		public string? CollectionId { get; set; }

		[JsonPropertyName("swisslex_comment")]
		public string? SwisslexComment { get; set; }
	}

	private sealed class AssetTypeInfo
	{
		[JsonPropertyName("book")]
		public BookInfo? Book { get; set; }

		[JsonPropertyName("court")]
		public CourtInfo? Court { get; set; }
	}

	private sealed class BookInfo
	{
		[JsonPropertyName("series")]
		public SeriesInfo? Series { get; set; }

		[JsonPropertyName("pages")]
		public string? Pages { get; set; }
	}

	private sealed class SeriesInfo
	{
		[JsonPropertyName("name")]
		public LanguageField? Name { get; set; }
	}

	private sealed class CourtInfo
	{
		[JsonPropertyName("location")]
		public string? Location { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("location_code")]
		public string? LocationCode { get; set; }
	}

	private sealed class LanguageField
	{
		[JsonPropertyName("de")]
		public string? De { get; set; }

		[JsonPropertyName("fr")]
		public string? Fr { get; set; }

		[JsonPropertyName("it")]
		public string? It { get; set; }

		[JsonPropertyName("en")]
		public string? En { get; set; }
	}

	private sealed class ContentObject
	{
		[JsonPropertyName("content")]
		public string? Content { get; set; }
	}

	private sealed class PublicationMeta
	{
		[JsonPropertyName("authors")]
		public List<Author>? Authors { get; set; }
	}

	private sealed class Author
	{
		[JsonPropertyName("full_names")]
		public string? FullName { get; set; }

		[JsonPropertyName("aliases")]
		public List<string>? Aliases { get; set; }
	}

	private sealed class LawReference
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("article")]
		public string? Article { get; set; }
	}

	private sealed class NavigationInfo
	{
		[JsonPropertyName("start_page")]
		public PageInfo? StartPage { get; set; }

		[JsonPropertyName("end_page")]
		public PageInfo? EndPage { get; set; }
	}

	private sealed class PageInfo
	{
		[JsonPropertyName("page_number")]
		public int PageNumber { get; set; }
	}

	// ── Tests mirroring SwissLex query patterns ──

	[Fact]
	public void MultiLanguage_Bool_Should_With_MinimumShouldMatch()
	{
		// Pattern: LawQueryExtensions — multilingual wildcard search
		var law = "OR";
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Should(
					sho => sho.Wildcard(f => f.AssetTypeInfo!.Book!.Series!.Name!.De!, w => w.Value($"*{law}*")),
					sho => sho.Wildcard(f => f.AssetTypeInfo!.Book!.Series!.Name!.Fr!, w => w.Value($"*{law}*")),
					sho => sho.Wildcard(f => f.AssetTypeInfo!.Book!.Series!.Name!.It!, w => w.Value($"*{law}*")),
					sho => sho.Wildcard(f => f.AssetTypeInfo!.Book!.Series!.Name!.En!, w => w.Value($"*{law}*"))
				)
				.MinimumShouldMatch(1)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var boolQ = doc.RootElement.GetProperty("query").GetProperty("bool");
		boolQ.GetProperty("should").GetArrayLength().Should().Be(4);
		boolQ.GetProperty("minimum_should_match").GetString().Should().Be("1");
	}

	[Fact]
	public void Range_Query_With_Date_Bounds()
	{
		// Pattern: BookQueryExtensions.BookYearQuery — date range filtering
		var dateFrom = new DateTime(2024, 1, 1);
		var dateUntil = new DateTime(2025, 1, 1);

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Filter(
					f => f.Range(p => p.Meta!.PublicationDate!, r => r.Gte(dateFrom.ToString("o"))),
					f => f.Range(p => p.Meta!.PublicationDate!, r => r.Lt(dateUntil.ToString("o")))
				)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var filter = doc.RootElement.GetProperty("query").GetProperty("bool").GetProperty("filter");
		filter.GetArrayLength().Should().Be(2);

		// First filter: range with gte
		var range1 = filter[0].GetProperty("range");
		range1.TryGetProperty("meta.publication_date", out var pubDate1).Should().BeTrue();
		pubDate1.TryGetProperty("gte", out _).Should().BeTrue();

		// Second filter: range with lt
		var range2 = filter[1].GetProperty("range");
		range2.TryGetProperty("meta.publication_date", out var pubDate2).Should().BeTrue();
		pubDate2.TryGetProperty("lt", out _).Should().BeTrue();
	}

	[Fact]
	public void Page_Range_Query_With_Bool_Should()
	{
		// Pattern: BookQueryExtensions.BookPagesQuery — page range with OR logic
		short page = 42;

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Should(
					s => s.Bool(bb => bb.Filter(
						m => m.Range(f => f.NavigationInfo!.StartPage!.PageNumber, r => r.Lte(page)),
						m => m.Range(f => f.NavigationInfo!.EndPage!.PageNumber, r => r.Gte(page))
					))
				)
				.MinimumShouldMatch(1)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var boolQ = doc.RootElement.GetProperty("query").GetProperty("bool");
		boolQ.GetProperty("should").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
		boolQ.GetProperty("minimum_should_match").GetString().Should().Be("1");
	}

	[Fact]
	public void Nested_Query_With_Suffix_Field()
	{
		// Pattern: BookQueryExtensions.BookAuthorQuery — nested author search
		var author = "müller";

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Nested(n => n
				.Path("publication_meta.authors")
				.Query(qu => qu.Term(
					"publication_meta.authors.aliases",
					t => t.Value(author)))
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var nested = doc.RootElement.GetProperty("query").GetProperty("nested");
		nested.GetProperty("path").GetString().Should().Be("publication_meta.authors");
		nested.TryGetProperty("query", out _).Should().BeTrue();
	}

	[Fact]
	public void MatchPhrasePrefix_MultiLanguage()
	{
		// Pattern: BookQueryExtensions.BookSeriesQuery (non-exact) — prefix matching
		var series = "commentaire";

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Should(
					sho => sho.MatchPhrasePrefix(
						f => f.AssetTypeInfo!.Book!.Series!.Name!.De!, w => w.Query(series)),
					sho => sho.MatchPhrasePrefix(
						f => f.AssetTypeInfo!.Book!.Series!.Name!.Fr!, w => w.Query(series))
				)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var should = doc.RootElement.GetProperty("query").GetProperty("bool").GetProperty("should");
		should.GetArrayLength().Should().Be(2);
		should[0].TryGetProperty("match_phrase_prefix", out _).Should().BeTrue();
		should[1].TryGetProperty("match_phrase_prefix", out _).Should().BeTrue();
	}

	[Fact]
	public void Bool_Must_Filter_MustNot_Composition()
	{
		// Pattern: QueryExtensions.BuildBasicBoolQuery — the main query orchestration
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Must(
					m => m.Match(f => f.ContentObject!.Content!, ma => ma.Query("vertrag"))
				)
				.Filter(
					f => f.Bool(inner => inner.Filter(
						ff => ff.Term(p => p.Meta!.DisplayType!, t => t.Value("case")),
						ff => ff.Term(p => p.Meta!.Language!, t => t.Value("de"))
					))
				)
				.MustNot(
					mn => mn.Term("search_visibility", t => t.Value(0L))
				)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var boolQ = doc.RootElement.GetProperty("query").GetProperty("bool");
		boolQ.GetProperty("must").GetArrayLength().Should().Be(1);
		boolQ.GetProperty("filter").GetArrayLength().Should().Be(1);
		boolQ.GetProperty("must_not").GetArrayLength().Should().Be(1);

		// Inner filter is a nested bool
		var innerBool = boolQ.GetProperty("filter")[0].GetProperty("bool");
		innerBool.GetProperty("filter").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void Terms_Aggregation_With_SubAgg_And_Order()
	{
		// Pattern: AggregationExtensions — court location → court name sub-agg
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Size(0)
			.Aggregations(a => a
				.Terms("court_location", t => t
					.Field("type_information.court.location.de.raw")
					.Size(50)
					.MinDocCount(1)
					.CountDescending(),
					sub => sub.Terms("court_name", stt => stt
						.Field("type_information.court.name.de.raw")
						.Size(50)
						.CountDescending())
				)
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var courtLoc = doc.RootElement
			.GetProperty("aggregations")
			.GetProperty("court_location");

		courtLoc.TryGetProperty("terms", out var terms).Should().BeTrue();
		terms.GetProperty("field").GetString().Should().Be("type_information.court.location.de.raw");
		terms.GetProperty("size").GetInt32().Should().Be(50);

		courtLoc.TryGetProperty("aggregations", out var subAggs).Should().BeTrue();
		subAggs.TryGetProperty("court_name", out _).Should().BeTrue();
	}

	[Fact]
	public void Nested_Aggregation_With_Filter_SubAgg()
	{
		// Pattern: AggregationExtensions.BuildCustomMetadataAggregation
		var metaName = "practice_area";

		var filterQuery = QueryContainer.Term("aggregations.custom_metadata.meta_name.keyword",
			new TermQuery { Value = JsonSerializer.SerializeToElement(metaName) });

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Size(0)
			.Aggregations(a => a
				.Nested(metaName, n => n.Path("aggregations.custom_metadata"),
					nestedSub => nestedSub
						.Filter(metaName, filterQuery,
							filteredSub => filteredSub
								.Terms(metaName, t => t
									.Field("aggregations.custom_metadata.meta_values.keyword")
									.Size(51)
									.MinDocCount(1)
									.CountDescending())))
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var practiceArea = doc.RootElement
			.GetProperty("aggregations")
			.GetProperty(metaName);

		practiceArea.TryGetProperty("nested", out var nested).Should().BeTrue();
		nested.GetProperty("path").GetString().Should().Be("aggregations.custom_metadata");

		// Sub-agg: filter → terms
		practiceArea.TryGetProperty("aggregations", out var subAggs).Should().BeTrue();
		subAggs.TryGetProperty(metaName, out var filterAgg).Should().BeTrue();
		filterAgg.TryGetProperty("filter", out _).Should().BeTrue();
		filterAgg.TryGetProperty("aggregations", out var innerSub).Should().BeTrue();
		innerSub.TryGetProperty(metaName, out var termsAgg).Should().BeTrue();
		termsAgg.TryGetProperty("terms", out _).Should().BeTrue();
	}

	[Fact]
	public void Highlight_FVH_With_MultiLanguage_MatchedFields()
	{
		// Pattern: QueryExtensions.BuildBasicHighlightQuery — FVH with language variants
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Highlight(h => h
				.Fields(
					("content.content", f => f
						.MatchedFields([
							"content.content.de",
							"content.content.fr",
							"content.content.it",
							"content.content.en"
						])
						.Type("fvh")
						.NumberOfFragments(0)
						.FragmentSize(140)
						.NoMatchSize(800))
				)
				.PreTags([
					"#highlightbegin1", "#highlightbegin2", "#highlightbegin3",
					"#highlightbegin4", "#highlightbegin5"
				])
				.PostTags([
					"#highlightend", "#highlightend", "#highlightend",
					"#highlightend", "#highlightend"
				])
			);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var highlight = doc.RootElement.GetProperty("highlight");
		highlight.GetProperty("pre_tags").GetArrayLength().Should().Be(5);
		highlight.GetProperty("post_tags").GetArrayLength().Should().Be(5);

		var contentField = highlight.GetProperty("fields").GetProperty("content.content");
		contentField.GetProperty("type").GetString().Should().Be("fvh");
		contentField.GetProperty("number_of_fragments").GetInt32().Should().Be(0);
		contentField.GetProperty("fragment_size").GetInt32().Should().Be(140);
		contentField.GetProperty("no_match_size").GetInt32().Should().Be(800);
		contentField.GetProperty("matched_fields").GetArrayLength().Should().Be(4);
	}

	[Fact]
	public void Full_Search_With_Query_Aggs_Highlight_Sort_Paging()
	{
		// Pattern: Combined query from SearchQueryService — the full production shape
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Index(["swisslex-assets"])
			.Size(20)
			.From(0)
			.Query(q => q.Bool(b => b
				.Must(
					m => m.Match(f => f.ContentObject!.Content!, ma => ma
						.Query("bundesgericht")
						.Operator(Operator.And))
				)
				.Filter(
					f => f.Term(p => p.Meta!.DisplayType!, t => t.Value("case")),
					f => f.Range(p => p.Meta!.PublicationDate!, r => r
						.Gte("2020-01-01")
						.Lt("2025-01-01"))
				)
			))
			.Aggregations(a => a
				.Terms("language", t => t
					.Field("meta.language")
					.Size(10)
					.CountDescending())
				.Terms("display_type", t => t
					.Field("meta.display_type")
					.Size(20)
					.CountDescending())
			)
			.Highlight(h => h
				.Fields(
					("content.content", f => f
						.MatchedFields([
							"content.content.de", "content.content.fr",
							"content.content.it", "content.content.en"
						])
						.Type("fvh")
						.NumberOfFragments(3)
						.FragmentSize(140)
						.NoMatchSize(200))
				)
				.PreTags(["<em>"])
				.PostTags(["</em>"])
			)
			.Sort(
				SortOptions.Descending("_score"),
				SortOptions.Descending("meta.publication_date")
			)
			.Source(SourceConfig.Enabled(false));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var root = doc.RootElement;

		// Verify structure
		root.GetProperty("size").GetInt32().Should().Be(20);
		root.GetProperty("from").GetInt32().Should().Be(0);

		// Query: bool with must + filter
		var boolQ = root.GetProperty("query").GetProperty("bool");
		boolQ.GetProperty("must").GetArrayLength().Should().Be(1);
		boolQ.GetProperty("filter").GetArrayLength().Should().Be(2);

		// Aggregations
		var aggs = root.GetProperty("aggregations");
		aggs.TryGetProperty("language", out _).Should().BeTrue();
		aggs.TryGetProperty("display_type", out _).Should().BeTrue();

		// Highlight
		var hl = root.GetProperty("highlight");
		hl.GetProperty("fields").TryGetProperty("content.content", out _).Should().BeTrue();

		// Sort
		root.GetProperty("sort").GetArrayLength().Should().Be(2);

		// Source filtering
		root.GetProperty("_source").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void Field_Expression_With_Deep_Nesting_And_JsonPropertyName()
	{
		// Verify expression resolution matches SwissLex field paths
		Field.From<ElasticAsset>(f => f.Meta!.PublicationDate!).Name
			.Should().Be("meta.publication_date");

		Field.From<ElasticAsset>(f => f.Meta!.DisplayType!).Name
			.Should().Be("meta.display_type");

		Field.From<ElasticAsset>(f => f.AssetTypeInfo!.Court!.LocationCode!).Name
			.Should().Be("type_information.court.location_code");

		Field.From<ElasticAsset>(f => f.ContentObject!.Content!).Name
			.Should().Be("content.content");

		Field.From<ElasticAsset>(f => f.NavigationInfo!.StartPage!.PageNumber).Name
			.Should().Be("navigation_information.start_page.page_number");
	}

	[Fact]
	public void Field_Expression_With_Suffix_For_Language_Variants()
	{
		// Pattern: SwissLex language + analyzer field suffixes
		Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("de")).Name
			.Should().Be("content.content.de");

		Field.From<ElasticAsset>(f => f.AssetTypeInfo!.Court!.Location!.Suffix("de").Suffix("raw")).Name
			.Should().Be("type_information.court.location.de.raw");
	}

	[Fact]
	public void ConstantScore_Filter_With_Term()
	{
		// Pattern: Restricted asset filtering — constant score wrapper
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.ConstantScore(cs => cs
				.Filter(f => f.Term(p => p.Meta!.DisplayType!, t => t.Value("law")))
				.Boost(1.0f)
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var cs = doc.RootElement.GetProperty("query").GetProperty("constant_score");
		cs.GetProperty("boost").GetSingle().Should().Be(1.0f);
		cs.TryGetProperty("filter", out _).Should().BeTrue();
	}

	// ── Gap-fix tests: Source(bool), FunctionScore, Terms expression, DateTime range ──

	[Fact]
	public void Source_Bool_False_Shorthand()
	{
		// Pattern: `.Source(false)` used ~25 times in SwissLex autocomplete queries
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Source(false)
			.Size(0);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		doc.RootElement.GetProperty("_source").GetBoolean().Should().BeFalse();
		doc.RootElement.GetProperty("size").GetInt32().Should().Be(0);
	}

	[Fact]
	public void Source_Bool_True_Shorthand()
	{
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Source(true);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		doc.RootElement.GetProperty("_source").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Range_With_DateTime_Values()
	{
		// Pattern: BookQueryExtensions — date range with DateTime objects
		var dateFrom = new DateTime(2023, 1, 1);
		var dateUntil = new DateTime(2024, 1, 1);

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Range(f => f.Meta!.PublicationDate!, r => r
				.Gte(dateFrom)
				.Lt(dateUntil)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var range = doc.RootElement.GetProperty("query").GetProperty("range")
			.GetProperty("meta.publication_date");
		range.TryGetProperty("gte", out _).Should().BeTrue();
		range.TryGetProperty("lt", out _).Should().BeTrue();
	}

	[Fact]
	public void Range_With_String_Now()
	{
		// Pattern: QueryExtensions.BuildPublishedDateFilter — `.Lte("now")`
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Range(f => f.Meta!.PublicationDate!, r => r
				.Lte("now")));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var range = doc.RootElement.GetProperty("query").GetProperty("range")
			.GetProperty("meta.publication_date");
		range.GetProperty("lte").GetString().Should().Be("now");
	}

	[Fact]
	public void Range_Conditional_DateTime_Nullable()
	{
		// Pattern: QueryExtensions.DateFilter — conditionally set gte/lt from nullable DateTime
		DateTime? dateFrom = new DateTime(2020, 6, 15);
		DateTime? dateUntil = null;

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Range(f => f.Meta!.PublicationDate!, r =>
			{
				if (dateFrom != null) r.Gte(dateFrom);
				if (dateUntil != null) r.Lt(dateUntil);
			}));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var range = doc.RootElement.GetProperty("query").GetProperty("range")
			.GetProperty("meta.publication_date");
		range.TryGetProperty("gte", out _).Should().BeTrue();
		range.TryGetProperty("lt", out _).Should().BeFalse();
	}

	[Fact]
	public void FunctionScore_On_Generic_QueryContainer()
	{
		// Pattern: QueryExtensions.CreateCompleteSearchQuery — FunctionScore wrapping the main query
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.FunctionScore(fs => fs
				.Query(innerQ => innerQ.MatchAll(new MatchAllQuery()))
				.Functions(
					f => f.FieldValueFactor(fvf => fvf
						.Field("cited_boost")
						.Modifier(FieldValueFactorModifier.Log1p)
						.Missing(1)),
					f => f.Exp(e => e
						.ExtensionData(new Dictionary<string, JsonElement>
						{
							["meta.publication_date"] = JsonSerializer.SerializeToElement(new
							{
								origin = "now",
								scale = "365d",
								decay = 0.5
							})
						}))
				)
				.ScoreMode(FunctionScoreMode.Sum)
				.BoostMode(FunctionBoostMode.Multiply)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var fs = doc.RootElement.GetProperty("query").GetProperty("function_score");
		fs.GetProperty("score_mode").GetString().Should().Be("sum");
		fs.GetProperty("boost_mode").GetString().Should().Be("multiply");
		fs.GetProperty("functions").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void Terms_With_Expression_Field()
	{
		// Pattern: QueryExtensions.AddSearchVisibilityFilters — terms query with expression field
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Terms(t => t
				.Field<ElasticAsset, object>(f => f.Rights!, (object)(short)1, (object)(short)2)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var terms = doc.RootElement.GetProperty("query").GetProperty("terms");
		terms.GetProperty("access_rights").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void Terms_With_Expression_Field_String_Values()
	{
		// Pattern: ContentRightsFilter — expression field with string values
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Terms(t => t
				.Field<ElasticAsset>(f => f.Rights!, "Standard", "Premium")));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var terms = doc.RootElement.GetProperty("query").GetProperty("terms");
		var rights = terms.GetProperty("access_rights");
		rights.GetArrayLength().Should().Be(2);
		rights[0].GetString().Should().Be("Standard");
		rights[1].GetString().Should().Be("Premium");
	}

	[Fact]
	public void Bool_Boost_On_Generic_Descriptor()
	{
		// Pattern: CreateContentQueryWrapper — Bool with Must + Boost for language boosting
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Must(m => m.MatchAll(new MatchAllQuery()))
				.Boost(2.0f)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var boolQ = doc.RootElement.GetProperty("query").GetProperty("bool");
		boolQ.GetProperty("boost").GetSingle().Should().Be(2.0f);
		boolQ.GetProperty("must").GetArrayLength().Should().Be(1);
	}

	[Fact]
	public void Collapse_With_InnerHits_And_Sort()
	{
		// Pattern: BookQueryExtensions.BuildBookAssetSearch — collapse by collection with inner hits
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Collapse(c => c
				.Field(Field.From<ElasticAsset>(o => o.Meta!.CollectionId!))
				.InnerHits(ih => ih
					.Name("book_subhits")
					.Size(100)
					.Source(SourceConfig.Filter(new SourceFilter
					{
						Includes = new List<string>
						{
							Field.From<ElasticAsset>(o => o.Title!),
							Field.From<ElasticAsset>(o => o.Meta!)
						}
					}))
					.Sort(new List<SortOptions>
					{
						SortOptions.Ascending(Field.From<ElasticAsset>(fi => fi.NavigationInfo!.StartPage!))
					})));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var collapse = doc.RootElement.GetProperty("collapse");
		collapse.GetProperty("field").GetString().Should().Be("meta.collection_id");

		var innerHits = collapse.GetProperty("inner_hits");
		innerHits.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
		var ih = innerHits[0];
		ih.GetProperty("name").GetString().Should().Be("book_subhits");
		ih.GetProperty("size").GetInt32().Should().Be(100);
	}

	[Fact]
	public void Aggregation_With_MinDocCount_And_Exclude()
	{
		// Pattern: AggregationExtensions.BuildAggregation — terms agg with min_doc_count and exclude
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Size(0)
			.Aggregations(a => a
				.Terms("display_type", t => t
					.Exclude(new List<string> { "LawDocument", "ArticleOfLawDocument" })
					.Field("display_type")
					.Size(51)
					.MinDocCount(0)
					.CountDescending()));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var agg = doc.RootElement.GetProperty("aggregations").GetProperty("display_type")
			.GetProperty("terms");
		agg.GetProperty("min_doc_count").GetInt64().Should().Be(0);
		agg.GetProperty("exclude").GetArrayLength().Should().Be(2);
		agg.GetProperty("size").GetInt32().Should().Be(51);
	}

	[Fact]
	public void Highlight_With_HighlightQuery_And_Order()
	{
		// Pattern: QueryExtensions.BuildHighlightingQuery — per-field highlight_query + order
		var contentField = Field.From<ElasticAsset>(x => x.ContentObject!.Content!);

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Highlight(h => h
				.Fields(
					((string)contentField, hf => hf
						.MatchedFields(new List<string>
						{
							Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("de")),
							Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("fr"))
						})
						.Type("fvh")
						.HighlightQuery(hq => hq.Bool(b => b
							.Should(s => s.SimpleQueryString(sq => sq
								.Query("search terms")
								.Fields(new List<string> { "content.content.de" })
								.DefaultOperator(Operator.And)))
							.MinimumShouldMatch("1")))
						.NumberOfFragments(3)
						.FragmentSize(140)
						.Order(HighlighterOrder.Score)
						.NoMatchSize(200)))
				.PreTags(new List<string> { "#highlightbegin1" })
				.PostTags(new List<string> { "#highlightend" }));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var fields = doc.RootElement.GetProperty("highlight").GetProperty("fields");
		fields.TryGetProperty("content.content", out var hf).Should().BeTrue();
		hf.GetProperty("type").GetString().Should().Be("fvh");
		hf.GetProperty("number_of_fragments").GetInt32().Should().Be(3);
		hf.GetProperty("order").GetString().Should().Be("score");
		hf.GetProperty("no_match_size").GetInt32().Should().Be(200);
		hf.TryGetProperty("highlight_query", out _).Should().BeTrue();
	}

	[Fact]
	public void Cardinality_Aggregation()
	{
		// Pattern: BookQueryExtensions — cardinality aggregation with precision_threshold
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Size(0)
			.Aggregations(a => a
				.Cardinality("unique_collections", c => c
					.Field(Field.From<ElasticAsset>(f => f.Meta!.CollectionId!))
					.PrecisionThreshold(40000)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var agg = doc.RootElement.GetProperty("aggregations").GetProperty("unique_collections")
			.GetProperty("cardinality");
		agg.GetProperty("field").GetString().Should().Be("meta.collection_id");
		agg.GetProperty("precision_threshold").GetInt32().Should().Be(40000);
	}

	[Fact]
	public void MultiMatch_Phrase_With_Slop()
	{
		// Pattern: QueryExtensions.CreatePhraseContentQuery — multi_match with type=phrase
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.MultiMatch(mm => mm
				.Query("exact phrase")
				.Fields(new List<string>
				{
					Field.From<ElasticAsset>(f => f.TitleAutocomplete!),
					Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("de"))
				})
				.Type(TextQueryType.Phrase)
				.Slop(0)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var mm = doc.RootElement.GetProperty("query").GetProperty("multi_match");
		mm.GetProperty("query").GetString().Should().Be("exact phrase");
		mm.GetProperty("type").GetString().Should().Be("phrase");
		mm.GetProperty("slop").GetInt32().Should().Be(0);
		mm.GetProperty("fields").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void SimpleQueryString_With_Analyzer_And_Boost()
	{
		// Pattern: QueryExtensions.CreateContentQuery — simpleQueryString per language
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Bool(b => b
				.Should(
					s => s.SimpleQueryString(sq => sq
						.Fields(new List<string> { "content.all.de" })
						.Analyzer("slx_german")
						.DefaultOperator(Operator.And)
						.Query("Bundesgericht")),
					s => s.SimpleQueryString(sq => sq
						.Fields(new List<string>
						{
							Field.From<ElasticAsset>(f => f.TitleAutocomplete!)
						})
						.Boost(5)
						.Analyzer("slx_german")
						.DefaultOperator(Operator.And)
						.Query("Bundesgericht"))
				)
				.MinimumShouldMatch(1)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var boolQ = doc.RootElement.GetProperty("query").GetProperty("bool");
		var shouldArr = boolQ.GetProperty("should");
		shouldArr.GetArrayLength().Should().Be(2);

		var sq1 = shouldArr[0].GetProperty("simple_query_string");
		sq1.GetProperty("analyzer").GetString().Should().Be("slx_german");
		sq1.GetProperty("default_operator").GetString().Should().Be("and");

		var sq2 = shouldArr[1].GetProperty("simple_query_string");
		sq2.GetProperty("boost").GetSingle().Should().Be(5);
	}

	[Fact]
	public void MatchPhrase_With_Slop_And_Analyzer()
	{
		// Pattern: QueryExtensions.CreatePhraseContentQuery — match_phrase with analyzer
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.MatchPhrase(
				f => f.ContentObject!.Content!.Suffix("de"),
				m => m.Query("Bundesgericht Urteil").Slop(0).Analyzer("slx_german_nostop")));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var mp = doc.RootElement.GetProperty("query").GetProperty("match_phrase")
			.GetProperty("content.content.de");
		mp.GetProperty("query").GetString().Should().Be("Bundesgericht Urteil");
		mp.GetProperty("slop").GetInt32().Should().Be(0);
		mp.GetProperty("analyzer").GetString().Should().Be("slx_german_nostop");
	}

	[Fact]
	public void Match_With_Operator_And()
	{
		// Pattern: LawQueryExtensions.LawTitleQuery — match with Operator.And
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Query(q => q.Match(
				f => f.AssetTypeInfo!.Book!.Series!.Name!.De!.Suffix("autocomplete"),
				m => m.Query("obligationenrecht").Operator(Operator.And)));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var match = doc.RootElement.GetProperty("query").GetProperty("match")
			.GetProperty("type_information.book.series.name.de.autocomplete");
		match.GetProperty("operator").GetString().Should().Be("and");
	}

	[Fact]
	public void HighlightQuery_With_Generic_QueryContainerDescriptor()
	{
		// Pattern: QueryExtensions.BuildHighlightingQuery — pass Action<QCD<T>> to .HighlightQuery()
		// Previously required pre-building a QueryContainerDescriptor<T> and casting to QueryContainer
		Action<QueryContainerDescriptor<ElasticAsset>> highlightQuery = q => q
			.Bool(b => b
				.Should(
					s => s.SimpleQueryString(sq => sq
						.Fields(new List<string> { "content.content.de" })
						.DefaultOperator(Operator.And)
						.Query("Bundesgericht")))
				.MinimumShouldMatch(1));

		var contentField = Field.From<ElasticAsset>(x => x.ContentObject!.Content!);

		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Highlight(h => h
				.Fields(
					((string)contentField, hf => hf
						.MatchedFields(new List<string>
						{
							Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("de")),
							Field.From<ElasticAsset>(f => f.ContentObject!.Content!.Suffix("fr"))
						})
						.Type("fvh")
						.HighlightQuery(highlightQuery)
						.NumberOfFragments(3)
						.FragmentSize(140)
						.Order(HighlighterOrder.Score)
						.NoMatchSize(200)))
				.PreTags(new List<string> { "#highlightbegin1" })
				.PostTags(new List<string> { "#highlightend" }));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var hf = doc.RootElement.GetProperty("highlight").GetProperty("fields")
			.GetProperty("content.content");
		hf.TryGetProperty("highlight_query", out var hq).Should().BeTrue();
		hq.GetProperty("bool").GetProperty("should").GetArrayLength().Should().Be(1);
	}

	[Fact]
	public void Suggest_Serializes_With_SnakeCase()
	{
		// Pattern: QueryExtensions.CreateSuggestQuery — term suggester with min_word_length
		// Without the SuggesterDescriptor.Add extension, JsonSerializer.SerializeToElement
		// uses PascalCase ("MinWordLength") and OpenSearch silently ignores it.
		SearchRequest request = new SearchRequestDescriptor<ElasticAsset>()
			.Suggest(s => s
				.Text("bundesgricht")
				.Add("content", new FieldSuggester
				{
					Term = new TermSuggester
					{
						Field = "content.all.de",
						Size = 2,
						MinWordLength = 5,
					},
					Text = "bundesgricht"
				}));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var suggest = doc.RootElement.GetProperty("suggest");
		suggest.GetProperty("text").GetString().Should().Be("bundesgricht");

		var content = suggest.GetProperty("content");
		var term = content.GetProperty("term");
		term.GetProperty("field").GetString().Should().Be("content.all.de");
		term.GetProperty("size").GetInt32().Should().Be(2);

		// This is the critical assertion — without the fix, this would be "MinWordLength" (PascalCase)
		term.GetProperty("min_word_length").GetInt32().Should().Be(5);
		term.TryGetProperty("MinWordLength", out _).Should().BeFalse("snake_case should be used, not PascalCase");
	}
}
