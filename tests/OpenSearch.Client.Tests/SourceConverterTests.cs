using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Client.Tests;

public class SourceConverterTests
{
	private sealed class MyDocument
	{
		public string? Title { get; set; }
		public int Score { get; set; }
	}

	/// <summary>
	/// A minimal source serializer that uses its own JsonSerializerOptions (simulating a user serializer).
	/// </summary>
	private sealed class TestSourceSerializer : IOpenSearchSerializer
	{
		private static readonly JsonSerializerOptions s_options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		public T? Deserialize<T>(Stream stream) =>
			JsonSerializer.Deserialize<T>(stream, s_options);

		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) =>
			JsonSerializer.DeserializeAsync<T>(stream, s_options, ct);

		public void Serialize<T>(T data, Stream stream) =>
			JsonSerializer.Serialize(stream, data, s_options);

		public async ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default) =>
			await JsonSerializer.SerializeAsync(stream, data, s_options, ct);
	}

	private static JsonSerializerOptions CreateOptionsWithSourceConverter(IOpenSearchSerializer sourceSerializer)
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			Converters = { new SourceConverter<MyDocument>() },
		};

		// Create a minimal mock settings that returns the source serializer.
		var settings = new TestClientSettings(sourceSerializer);
		options.Converters.Add(new ContextProvider<IOpenSearchClientSettings>(settings));

		return options;
	}

	/// <summary>
	/// Minimal IOpenSearchClientSettings implementation for testing.
	/// </summary>
	private sealed class TestClientSettings : IOpenSearchClientSettings
	{
		public TestClientSettings(IOpenSearchSerializer sourceSerializer)
		{
			SourceSerializer = sourceSerializer;
			// Use the same serializer for request/response as well.
			RequestResponseSerializer = sourceSerializer;
			RequestResponseOptions = new JsonSerializerOptions();
		}

		public IOpenSearchSerializer RequestResponseSerializer { get; }
		public IOpenSearchSerializer SourceSerializer { get; }
		public JsonSerializerOptions RequestResponseOptions { get; }
		public NodePool NodePool => new(new[] { new Uri("http://localhost:9200") });
		public IOpenSearchSerializer? Serializer => RequestResponseSerializer;
		public TimeSpan RequestTimeout => TimeSpan.FromSeconds(60);
		public TimeSpan DnsRefreshTimeout => TimeSpan.FromMinutes(5);
		public int MaxRetries => 0;
		public bool EnableHttpCompression => false;
		public BasicAuthCredentials? BasicAuth => null;
		public ApiKeyCredentials? ApiKeyAuth => null;
		public bool DisableAutomaticProxyDetection => false;
		public Uri? ProxyAddress => null;
		public string? ProxyUsername => null;
		public string? ProxyPassword => null;
		public Action<HttpRequestMessage>? OnRequestCreated => null;
	}

	[Fact]
	public void Read_DelegatesToSourceSerializer()
	{
		var sourceSerializer = new TestSourceSerializer();
		var options = CreateOptionsWithSourceConverter(sourceSerializer);

		// JSON with camelCase property names (matching the source serializer).
		var json = """{"title":"My Doc","score":42}"""u8;

		var result = JsonSerializer.Deserialize<MyDocument>(json, options);

		result.Should().NotBeNull();
		result!.Title.Should().Be("My Doc");
		result.Score.Should().Be(42);
	}

	[Fact]
	public void Write_DelegatesToSourceSerializer()
	{
		var sourceSerializer = new TestSourceSerializer();
		var options = CreateOptionsWithSourceConverter(sourceSerializer);

		var doc = new MyDocument { Title = "Hello", Score = 10 };

		var json = JsonSerializer.Serialize(doc, options);

		// The source serializer uses camelCase, so the output should reflect that.
		json.Should().Contain("\"title\"");
		json.Should().Contain("\"score\"");
		json.Should().Contain("\"Hello\"");
	}

	[Fact]
	public void Read_ThrowsInvalidOperationException_WhenContextProviderIsMissing()
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new SourceConverter<MyDocument>() },
		};

		var jsonBytes = """{"title":"test","score":1}"""u8.ToArray();

		var act = () => JsonSerializer.Deserialize<MyDocument>(jsonBytes, options);

		act.Should().Throw<InvalidOperationException>()
			.Which.Message.Should().Contain("IOpenSearchClientSettings");
	}

	[Fact]
	public void Write_ThrowsInvalidOperationException_WhenContextProviderIsMissing()
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new SourceConverter<MyDocument>() },
		};

		var doc = new MyDocument { Title = "test", Score = 1 };

		var act = () => JsonSerializer.Serialize(doc, options);

		act.Should().Throw<InvalidOperationException>()
			.Which.Message.Should().Contain("IOpenSearchClientSettings");
	}
}
