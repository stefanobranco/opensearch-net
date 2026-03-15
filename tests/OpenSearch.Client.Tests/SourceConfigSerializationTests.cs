using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SourceConfigSerializationTests
{
	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};
		return options;
	}

	[Fact]
	public void SourceConfig_Bool_True_SerializesAsBool()
	{
		SourceConfig config = true;
		var json = JsonSerializer.Serialize(config, JsonOptions);
		json.Should().Be("true");
	}

	[Fact]
	public void SourceConfig_Bool_False_SerializesAsBool()
	{
		SourceConfig config = false;
		var json = JsonSerializer.Serialize(config, JsonOptions);
		json.Should().Be("false");
	}

	[Fact]
	public void SourceConfig_Filter_SerializesAsObject()
	{
		var config = SourceConfig.Filter(new SourceFilter { Includes = ["title", "author"] });
		var json = JsonSerializer.Serialize(config, JsonOptions);
		var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("includes", out var includesEl).Should().BeTrue();
		includesEl.GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void SourceConfig_Deserialize_Bool()
	{
		var config = JsonSerializer.Deserialize<SourceConfig>("true", JsonOptions);
		config.Should().NotBeNull();
		config!.IsBool.Should().BeTrue();
		config.AsBool().Should().BeTrue();
	}

	[Fact]
	public void SourceConfig_Deserialize_Filter()
	{
		var json = """{"includes":["title"],"excludes":["internal"]}""";
		var config = JsonSerializer.Deserialize<SourceConfig>(json, JsonOptions);

		config.Should().NotBeNull();
		config!.IsFilter.Should().BeTrue();
		config.AsFilter().Includes.Should().Contain("title");
		config.AsFilter().Excludes.Should().Contain("internal");
	}

	[Fact]
	public void SourceConfig_RoundTrip_Bool()
	{
		SourceConfig original = false;
		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<SourceConfig>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.IsBool.Should().BeTrue();
		deserialized.AsBool().Should().BeFalse();
	}

	[Fact]
	public void SourceConfig_RoundTrip_Filter()
	{
		var original = SourceConfig.Filter(new SourceFilter
		{
			Includes = ["name", "age"],
			Excludes = ["password"]
		});

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<SourceConfig>(json, JsonOptions);

		deserialized.Should().NotBeNull();
		deserialized!.IsFilter.Should().BeTrue();
		deserialized.AsFilter().Includes.Should().BeEquivalentTo(["name", "age"]);
		deserialized.AsFilter().Excludes.Should().BeEquivalentTo(["password"]);
	}
}
