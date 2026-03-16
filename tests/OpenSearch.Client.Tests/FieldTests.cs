using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class FieldTests
{
	// Test document type
	private sealed class TestDocument
	{
		public string? Title { get; set; }
		public int Price { get; set; }
		public MetaInfo? Meta { get; set; }

		[JsonPropertyName("custom_name")]
		public string? OverriddenField { get; set; }
	}

	private sealed class MetaInfo
	{
		public string? DisplayType { get; set; }

		[JsonPropertyName("display_order")]
		public int Order { get; set; }
	}

	[Fact]
	public void String_Construction()
	{
		var field = new Field("status");
		field.Name.Should().Be("status");
		field.ToString().Should().Be("status");
	}

	[Fact]
	public void Implicit_Conversion_From_String()
	{
		Field field = "status";
		field.Name.Should().Be("status");
	}

	[Fact]
	public void Implicit_Conversion_To_String()
	{
		var field = new Field("status");
		string name = field;
		name.Should().Be("status");
	}

	[Fact]
	public void Expression_Simple_Property()
	{
		var field = Field.From<TestDocument>(f => f.Title!);
		field.Name.Should().Be("title");
	}

	[Fact]
	public void Expression_Value_Type_Property()
	{
		var field = Field.From<TestDocument>(f => f.Price);
		field.Name.Should().Be("price");
	}

	[Fact]
	public void Expression_Nested_Property()
	{
		var field = Field.From<TestDocument>(f => f.Meta!.DisplayType!);
		field.Name.Should().Be("meta.display_type");
	}

	[Fact]
	public void Expression_With_JsonPropertyName_Override()
	{
		var field = Field.From<TestDocument>(f => f.OverriddenField!);
		field.Name.Should().Be("custom_name");
	}

	[Fact]
	public void Expression_Nested_With_JsonPropertyName()
	{
		var field = Field.From<TestDocument>(f => f.Meta!.Order);
		field.Name.Should().Be("meta.display_order");
	}

	[Fact]
	public void Suffix_Method()
	{
		var field = new Field("name");
		var suffixed = field.Suffix("keyword");
		suffixed.Name.Should().Be("name.keyword");
	}

	[Fact]
	public void Expression_With_Suffix()
	{
		var field = Field.From<TestDocument>(f => f.Title!.Suffix("keyword"));
		field.Name.Should().Be("title.keyword");
	}

	[Fact]
	public void Expression_With_Variable_Suffix()
	{
		var lang = "de";
		var field = Field.From<TestDocument>(f => f.Title!.Suffix(lang));
		field.Name.Should().Be("title.de");
	}

	[Fact]
	public void Expression_With_Chained_Variable_And_Literal_Suffix()
	{
		var lang = "fr";
		var field = Field.From<TestDocument>(f => f.Title!.Suffix(lang).Suffix("raw"));
		field.Name.Should().Be("title.fr.raw");
	}

	[Fact]
	public void Expression_With_Multiple_Variable_Suffixes()
	{
		var lang = "en";
		var analyzer = "stemmed";
		var field = Field.From<TestDocument>(f => f.Title!.Suffix(lang).Suffix(analyzer));
		field.Name.Should().Be("title.en.stemmed");
	}

	[Fact]
	public void Serialization_RoundTrip()
	{
		var field = new Field("status");
		var json = JsonSerializer.Serialize(field);
		json.Should().Be("\"status\"");

		var deserialized = JsonSerializer.Deserialize<Field>(json);
		deserialized.Should().NotBeNull();
		deserialized!.Name.Should().Be("status");
	}

	[Fact]
	public void Equality()
	{
		var a = new Field("status");
		var b = new Field("status");
		a.Equals(b).Should().BeTrue();
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	[Fact]
	public void Inequality()
	{
		var a = new Field("status");
		var b = new Field("price");
		a.Equals(b).Should().BeFalse();
	}
}
