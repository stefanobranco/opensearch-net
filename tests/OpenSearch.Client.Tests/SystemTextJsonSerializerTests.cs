using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SystemTextJsonSerializerTests
{
	private static SystemTextJsonSerializer CreateSerializer()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		return new SystemTextJsonSerializer(options);
	}

	private sealed class TestPoco
	{
		public string? MyField { get; set; }
		public int? Count { get; set; }
		public string? NullableField { get; set; }
	}

	[Fact]
	public void RoundTrip_SerializeAndDeserialize_Poco()
	{
		var serializer = CreateSerializer();
		var original = new TestPoco { MyField = "hello", Count = 42 };

		using var stream = new MemoryStream();
		serializer.Serialize(original, stream);
		stream.Position = 0;

		var result = serializer.Deserialize<TestPoco>(stream);

		result.Should().NotBeNull();
		result!.MyField.Should().Be("hello");
		result.Count.Should().Be(42);
	}

	[Fact]
	public void Serialize_UsesSnakeCasePropertyNaming()
	{
		var serializer = CreateSerializer();
		var poco = new TestPoco { MyField = "test" };

		using var stream = new MemoryStream();
		serializer.Serialize(poco, stream);
		stream.Position = 0;

		var json = new StreamReader(stream).ReadToEnd();
		json.Should().Contain("\"my_field\"");
		json.Should().NotContain("\"MyField\"");
	}

	[Fact]
	public void Serialize_OmitsNullProperties()
	{
		var serializer = CreateSerializer();
		var poco = new TestPoco { MyField = "present", NullableField = null };

		using var stream = new MemoryStream();
		serializer.Serialize(poco, stream);
		stream.Position = 0;

		var json = new StreamReader(stream).ReadToEnd();
		json.Should().Contain("\"my_field\"");
		json.Should().NotContain("\"nullable_field\"");
	}

	[Fact]
	public void Deserialize_AllowsReadingNumbersFromStrings()
	{
		var serializer = CreateSerializer();
		var json = """{"my_field":"value","count":"99"}"""u8;

		using var stream = new MemoryStream(json.ToArray());
		var result = serializer.Deserialize<TestPoco>(stream);

		result.Should().NotBeNull();
		result!.Count.Should().Be(99);
	}

	[Fact]
	public void Deserialize_EmptyStream_ReturnsDefault()
	{
		var serializer = CreateSerializer();
		using var stream = new MemoryStream([]);

		var result = serializer.Deserialize<TestPoco>(stream);

		result.Should().BeNull();
	}

	[Fact]
	public async Task Async_RoundTrip_SerializeAndDeserialize()
	{
		var serializer = CreateSerializer();
		var original = new TestPoco { MyField = "async_test", Count = 7 };

		using var stream = new MemoryStream();
		await serializer.SerializeAsync(original, stream);
		stream.Position = 0;

		var result = await serializer.DeserializeAsync<TestPoco>(stream);

		result.Should().NotBeNull();
		result!.MyField.Should().Be("async_test");
		result.Count.Should().Be(7);
	}

	[Fact]
	public async Task DeserializeAsync_EmptyStream_ReturnsDefault()
	{
		var serializer = CreateSerializer();
		using var stream = new MemoryStream([]);

		var result = await serializer.DeserializeAsync<TestPoco>(stream);

		result.Should().BeNull();
	}
}
