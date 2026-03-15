using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class GenericDescriptorTests
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
		public string? Title { get; set; }
		public string? Status { get; set; }
		public double Price { get; set; }

		[JsonPropertyName("category_id")]
		public int CategoryId { get; set; }
	}

	[Fact]
	public void Term_With_Expression_Resolves_Field()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.Term(f => f.Status!, t => t.Value("active")));

		request.Query.Should().NotBeNull();
		request.Query!.Kind.Should().Be(QueryKind.Term);

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.TryGetProperty("query", out var query).Should().BeTrue();
		query.TryGetProperty("term", out var term).Should().BeTrue();
		term.TryGetProperty("status", out _).Should().BeTrue();
	}

	[Fact]
	public void Term_With_JsonPropertyName_Uses_Custom_Name()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.Term(f => f.CategoryId, t => t.Value("42")));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.TryGetProperty("query", out var query).Should().BeTrue();
		query.TryGetProperty("term", out var term).Should().BeTrue();
		term.TryGetProperty("category_id", out _).Should().BeTrue();
	}

	[Fact]
	public void Match_With_Expression()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.Match(f => f.Title!, m => m.Query("laptop")));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.TryGetProperty("query", out var query).Should().BeTrue();
		query.TryGetProperty("match", out var match).Should().BeTrue();
		match.TryGetProperty("title", out var titleMatch).Should().BeTrue();
		titleMatch.TryGetProperty("query", out var matchQuery).Should().BeTrue();
		matchQuery.GetString().Should().Be("laptop");
	}

	[Fact]
	public void Bool_Must_Term_With_Expression()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.Bool(b => b
				.Must(
					m => m.Term(f => f.Status!, t => t.Value("active"))
				)
			));

		request.Query.Should().NotBeNull();
		request.Query!.Kind.Should().Be(QueryKind.Bool);
		var boolQuery = request.Query.Get<BoolQuery>();
		boolQuery.Must.Should().HaveCount(1);
		boolQuery.Must![0].Kind.Should().Be(QueryKind.Term);
	}

	[Fact]
	public void Bool_Filter_Multiple_With_Expression()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.Bool(b => b
				.Filter(
					f => f.Term(p => p.Status!, t => t.Value("active")),
					f => f.Exists(p => p.Title!)
				)
			));

		var boolQuery = request.Query!.Get<BoolQuery>();
		boolQuery.Filter.Should().HaveCount(2);
		boolQuery.Filter![0].Kind.Should().Be(QueryKind.Term);
		boolQuery.Filter[1].Kind.Should().Be(QueryKind.Exists);
	}

	[Fact]
	public void ConstantScore_With_Expression()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.ConstantScore(cs => cs
				.Boost(1.5f)
				.Filter(f => f.Term(p => p.Status!, t => t.Value("active")))
			));

		request.Query!.Kind.Should().Be(QueryKind.ConstantScore);
		var csQuery = request.Query.Get<ConstantScoreQuery>();
		csQuery.Boost.Should().Be(1.5f);
		csQuery.Filter.Should().NotBeNull();
		csQuery.Filter!.Kind.Should().Be(QueryKind.Term);
	}

	[Fact]
	public void MatchAll_Query()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Query(q => q.MatchAll(m => { }));

		request.Query!.Kind.Should().Be(QueryKind.MatchAll);
	}

	[Fact]
	public void SearchRequest_With_Size_From_Sort()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Size(10)
			.From(20)
			.Sort(SortOptions.Descending("price"))
			.Query(q => q.MatchAll(new MatchAllQuery()));

		request.Size.Should().Be(10);
		request.From.Should().Be(20);
		request.Sort.Should().HaveCount(1);
		request.Query.Should().NotBeNull();
	}

	[Fact]
	public void Full_SearchRequest_Serialization()
	{
		SearchRequest request = new SearchRequestDescriptor<Product>()
			.Size(10)
			.Query(q => q.Bool(b => b
				.Must(m => m.Match(f => f.Title!, ma => ma.Query("laptop")))
				.Filter(f => f.Term(p => p.Status!, t => t.Value("active")))
			));

		var json = JsonSerializer.Serialize(request, JsonOptions);
		using var doc = JsonDocument.Parse(json);

		var root = doc.RootElement;
		root.TryGetProperty("size", out var size).Should().BeTrue();
		size.GetInt32().Should().Be(10);

		root.TryGetProperty("query", out var query).Should().BeTrue();
		query.TryGetProperty("bool", out var boolQ).Should().BeTrue();
		boolQ.TryGetProperty("must", out var must).Should().BeTrue();
		must.GetArrayLength().Should().Be(1);
		boolQ.TryGetProperty("filter", out var filter).Should().BeTrue();
		filter.GetArrayLength().Should().Be(1);
	}
}
