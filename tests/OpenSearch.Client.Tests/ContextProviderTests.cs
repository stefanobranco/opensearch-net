using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class ContextProviderTests
{
	private sealed class TestContext
	{
		public string Name { get; set; } = "test";
	}

	[Fact]
	public void Get_ReturnsContext_WhenProviderIsInConvertersList()
	{
		var context = new TestContext { Name = "found" };
		var options = new JsonSerializerOptions();
		options.Converters.Add(new ContextProvider<TestContext>(context));

		var result = ContextProvider<TestContext>.Get(options);

		result.Should().NotBeNull();
		result!.Name.Should().Be("found");
	}

	[Fact]
	public void Get_ReturnsNull_WhenProviderIsNotInConvertersList()
	{
		var options = new JsonSerializerOptions();

		var result = ContextProvider<TestContext>.Get(options);

		result.Should().BeNull();
	}

	[Fact]
	public void CanConvert_AlwaysReturnsFalse()
	{
		var provider = new ContextProvider<TestContext>(new TestContext());

		provider.CanConvert(typeof(string)).Should().BeFalse();
		provider.CanConvert(typeof(int)).Should().BeFalse();
		provider.CanConvert(typeof(TestContext)).Should().BeFalse();
		provider.CanConvert(typeof(object)).Should().BeFalse();
	}

	[Fact]
	public void CreateConverter_ReturnsNull()
	{
		var provider = new ContextProvider<TestContext>(new TestContext());
		var options = new JsonSerializerOptions();

		var converter = provider.CreateConverter(typeof(string), options);

		converter.Should().BeNull();
	}
}
