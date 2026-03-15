using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Client.Common;
using OpenSearch.Client.Core;
using Xunit;

namespace OpenSearch.Client.Tests;

public class TermsExpressionTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Converters = { new JsonEnumConverterFactory() },
	};

	private sealed class MyDoc
	{
		public string? Status { get; set; }
		public string? Category { get; set; }
		public int? Priority { get; set; }
	}

	[Fact]
	public void Terms_ExpressionWithStringValues_SerializesCorrectly()
	{
		var desc = new QueryContainerDescriptor<MyDoc>();
		desc.Terms(f => f.Status!, "active", "pending");

		QueryContainer? query = desc;
		query.Should().NotBeNull();
		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("terms", out var termsEl).Should().BeTrue();
		termsEl.TryGetProperty("status", out var valuesEl).Should().BeTrue();
		valuesEl.GetArrayLength().Should().Be(2);
		valuesEl[0].GetString().Should().Be("active");
		valuesEl[1].GetString().Should().Be("pending");
	}

	[Fact]
	public void Terms_ExpressionWithTypedValues_SerializesCorrectly()
	{
		var desc = new QueryContainerDescriptor<MyDoc>();
		desc.Terms<int>(f => f.Priority!, 1, 2, 3);

		QueryContainer? query = desc;
		query.Should().NotBeNull();
		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		var termsEl = doc.RootElement.GetProperty("terms");
		termsEl.TryGetProperty("priority", out var valuesEl).Should().BeTrue();
		valuesEl.GetArrayLength().Should().Be(3);
		valuesEl[0].GetInt32().Should().Be(1);
	}

	[Fact]
	public void Terms_StringFieldWithValues_SerializesCorrectly()
	{
		var desc = new QueryContainerDescriptor<MyDoc>();
		desc.Terms("category", "books", "electronics");

		QueryContainer? query = desc;
		query.Should().NotBeNull();
		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		var termsEl = doc.RootElement.GetProperty("terms");
		termsEl.TryGetProperty("category", out var valuesEl).Should().BeTrue();
		valuesEl.GetArrayLength().Should().Be(2);
		valuesEl[0].GetString().Should().Be("books");
	}

	[Fact]
	public void Terms_ViaSearchDescriptor_WorksEndToEnd()
	{
		var searchDesc = new SearchRequestDescriptor<MyDoc>();
		searchDesc.Query(q => q.Terms(f => f.Status!, "active", "closed"));

		SearchRequest request = searchDesc;
		var json = JsonSerializer.Serialize(request, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("query", out var queryEl).Should().BeTrue();
		queryEl.TryGetProperty("terms", out var termsEl).Should().BeTrue();
		termsEl.TryGetProperty("status", out var vals).Should().BeTrue();
		vals.GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void Terms_ExpressionResolvesJsonPropertyName()
	{
		// Verify expression resolution respects the property name (snake_case default)
		var desc = new QueryContainerDescriptor<MyDoc>();
		desc.Terms(f => f.Category!, "a", "b");

		QueryContainer? query = desc;
		query.Should().NotBeNull();
		var json = JsonSerializer.Serialize(query, JsonOptions);

		// "Category" should resolve to "category" via snake_case
		json.Should().Contain("\"category\"");
	}

	[Fact]
	public void Terms_DescriptorOverload_StillWorks()
	{
		var desc = new QueryContainerDescriptor<MyDoc>();
		desc.Terms(t => t
			.Field<MyDoc>(f => f.Status!, "active", "pending")
			.Boost(1.5f));

		QueryContainer? query = desc;
		query.Should().NotBeNull();
		var json = JsonSerializer.Serialize(query, JsonOptions);
		var doc = JsonDocument.Parse(json);

		var termsEl = doc.RootElement.GetProperty("terms");
		termsEl.TryGetProperty("status", out _).Should().BeTrue();
		termsEl.GetProperty("boost").GetSingle().Should().Be(1.5f);
	}
}
