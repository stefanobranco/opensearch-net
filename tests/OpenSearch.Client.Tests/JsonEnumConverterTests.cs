using System.Runtime.Serialization;
using System.Text.Json;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

[JsonEnum]
public enum TestStatus
{
	[EnumMember(Value = "green")]
	Green,

	[EnumMember(Value = "yellow")]
	Yellow,

	[EnumMember(Value = "red")]
	Red,
}

public class JsonEnumConverterTests
{
	private static readonly JsonSerializerOptions s_options = new()
	{
		Converters = { new JsonEnumConverterFactory() },
	};

	[Theory]
	[InlineData(TestStatus.Green, "\"green\"")]
	[InlineData(TestStatus.Yellow, "\"yellow\"")]
	[InlineData(TestStatus.Red, "\"red\"")]
	public void Serialize_UsesEnumMemberValue(TestStatus value, string expectedJson)
	{
		var json = JsonSerializer.Serialize(value, s_options);

		json.Should().Be(expectedJson);
	}

	[Theory]
	[InlineData("\"green\"", TestStatus.Green)]
	[InlineData("\"yellow\"", TestStatus.Yellow)]
	[InlineData("\"red\"", TestStatus.Red)]
	public void Deserialize_ReadsEnumMemberValue(string json, TestStatus expected)
	{
		var result = JsonSerializer.Deserialize<TestStatus>(json, s_options);

		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("\"GREEN\"", TestStatus.Green)]
	[InlineData("\"Green\"", TestStatus.Green)]
	[InlineData("\"RED\"", TestStatus.Red)]
	public void Deserialize_IsCaseInsensitive(string json, TestStatus expected)
	{
		var result = JsonSerializer.Deserialize<TestStatus>(json, s_options);

		result.Should().Be(expected);
	}

	[Fact]
	public void Deserialize_ThrowsJsonException_ForUnknownValue()
	{
		var act = () => JsonSerializer.Deserialize<TestStatus>("\"purple\"", s_options);

		act.Should().Throw<JsonException>()
			.Which.Message.Should().Contain("purple");
	}

	[Fact]
	public void RoundTrip_SerializeAndDeserialize()
	{
		var original = TestStatus.Yellow;

		var json = JsonSerializer.Serialize(original, s_options);
		var result = JsonSerializer.Deserialize<TestStatus>(json, s_options);

		result.Should().Be(original);
	}

	[Fact]
	public void CanConvert_ReturnsTrueForAnnotatedEnum()
	{
		var factory = new JsonEnumConverterFactory();

		factory.CanConvert(typeof(TestStatus)).Should().BeTrue();
	}

	[Fact]
	public void CanConvert_ReturnsFalseForUnannotatedEnum()
	{
		var factory = new JsonEnumConverterFactory();

		factory.CanConvert(typeof(DayOfWeek)).Should().BeFalse();
	}

	[Fact]
	public void CanConvert_ReturnsFalseForNonEnumType()
	{
		var factory = new JsonEnumConverterFactory();

		factory.CanConvert(typeof(string)).Should().BeFalse();
	}
}
