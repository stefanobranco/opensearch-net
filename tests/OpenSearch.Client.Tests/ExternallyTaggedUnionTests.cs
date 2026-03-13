using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class ExternallyTaggedUnionTests
{
	[Fact]
	public void ParseKey_WithHashSeparator_ReturnsTypeAndName()
	{
		var (type, name) = ExternallyTaggedUnion.ParseKey("sterms#my_colors");

		type.Should().Be("sterms");
		name.Should().Be("my_colors");
	}

	[Fact]
	public void ParseKey_WithoutHash_ReturnsKeyAsType_AndNullName()
	{
		var (type, name) = ExternallyTaggedUnion.ParseKey("some_key");

		type.Should().Be("some_key");
		name.Should().BeNull();
	}

	[Theory]
	[InlineData("avg#price", "avg", "price")]
	[InlineData("terms#colors", "terms", "colors")]
	[InlineData("nested#inner.outer", "nested", "inner.outer")]
	public void ParseKey_VariousInputs(string key, string expectedType, string expectedName)
	{
		var (type, name) = ExternallyTaggedUnion.ParseKey(key);

		type.Should().Be(expectedType);
		name.Should().Be(expectedName);
	}

	[Fact]
	public void ParseKey_WithEmptyName_ReturnsEmptyString()
	{
		// "type#" => type="type", name=""
		var (type, name) = ExternallyTaggedUnion.ParseKey("type#");

		type.Should().Be("type");
		name.Should().Be("");
	}

	[Fact]
	public void BuildKey_WithName_ReturnsTypeHashName()
	{
		var result = ExternallyTaggedUnion.BuildKey("sterms", "my_colors");

		result.Should().Be("sterms#my_colors");
	}

	[Fact]
	public void BuildKey_WithoutName_ReturnsJustType()
	{
		var result = ExternallyTaggedUnion.BuildKey("sterms", null);

		result.Should().Be("sterms");
	}

	[Theory]
	[InlineData("avg", "price", "avg#price")]
	[InlineData("terms", "colors", "terms#colors")]
	[InlineData("nested", null, "nested")]
	public void BuildKey_VariousInputs(string type, string? name, string expected)
	{
		var result = ExternallyTaggedUnion.BuildKey(type, name);

		result.Should().Be(expected);
	}

	[Fact]
	public void RoundTrip_ParseKey_ThenBuildKey()
	{
		var original = "sterms#my_agg";

		var (type, name) = ExternallyTaggedUnion.ParseKey(original);
		var rebuilt = ExternallyTaggedUnion.BuildKey(type, name);

		rebuilt.Should().Be(original);
	}
}
