using System.Text;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class RequestBodyTests
{
	[Fact]
	public void JsonBody_HasApplicationJsonContentType()
	{
		var body = RequestBody.Json(new { name = "test" });
		body.ContentType.Should().Be("application/json");
	}

	[Fact]
	public void JsonBody_SerializesUsingSerializer()
	{
		var serializer = new TrackingSerializer();
		var body = RequestBody.Json(new { name = "test" });

		using var stream = new MemoryStream();
		body.WriteTo(stream, serializer);

		serializer.SerializeCallCount.Should().Be(1);
	}

	[Fact]
	public async Task JsonBody_SerializesAsyncUsingSerializer()
	{
		var serializer = new TrackingSerializer();
		var body = RequestBody.Json(new { name = "test" });

		using var stream = new MemoryStream();
		await body.WriteToAsync(stream, serializer);

		serializer.SerializeAsyncCallCount.Should().Be(1);
	}

	[Fact]
	public void RawBody_WritesExactBytes()
	{
		var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		var body = RequestBody.Raw(bytes);

		using var stream = new MemoryStream();
		body.WriteTo(stream, new NoOpSerializer());

		stream.ToArray().Should().Equal(bytes);
	}

	[Fact]
	public void RawBody_PreservesContentType()
	{
		var body = RequestBody.Raw(new byte[] { 1, 2 }, "application/cbor");
		body.ContentType.Should().Be("application/cbor");
	}

	[Fact]
	public void RawBody_DefaultContentTypeIsOctetStream()
	{
		var body = RequestBody.Raw(new byte[] { 1, 2 });
		body.ContentType.Should().Be("application/octet-stream");
	}

	[Fact]
	public async Task RawBody_WritesExactBytesAsync()
	{
		var bytes = new byte[] { 0xAA, 0xBB, 0xCC };
		var body = RequestBody.Raw(bytes);

		using var stream = new MemoryStream();
		await body.WriteToAsync(stream, new NoOpSerializer());

		stream.ToArray().Should().Equal(bytes);
	}

	[Fact]
	public void StringBody_WritesStringContent()
	{
		var content = "Hello, OpenSearch!";
		var body = RequestBody.String(content);

		using var stream = new MemoryStream();
		body.WriteTo(stream, new NoOpSerializer());

		var result = Encoding.UTF8.GetString(stream.ToArray());
		result.Should().Be(content);
	}

	[Fact]
	public void StringBody_HasCorrectContentType()
	{
		var body = RequestBody.String("content", "application/x-ndjson");
		body.ContentType.Should().Be("application/x-ndjson");
	}

	[Fact]
	public void StringBody_DefaultContentTypeIsTextPlain()
	{
		var body = RequestBody.String("content");
		body.ContentType.Should().Be("text/plain");
	}

	[Fact]
	public async Task StringBody_WritesStringContentAsync()
	{
		var content = "Async content";
		var body = RequestBody.String(content);

		using var stream = new MemoryStream();
		await body.WriteToAsync(stream, new NoOpSerializer());

		var result = Encoding.UTF8.GetString(stream.ToArray());
		result.Should().Be(content);
	}

	private sealed class TrackingSerializer : IOpenSearchSerializer
	{
		public int SerializeCallCount { get; private set; }
		public int SerializeAsyncCallCount { get; private set; }

		public T? Deserialize<T>(Stream stream) => default;
		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) => default;

		public void Serialize<T>(T data, Stream stream) => SerializeCallCount++;

		public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default)
		{
			SerializeAsyncCallCount++;
			return default;
		}
	}

	private sealed class NoOpSerializer : IOpenSearchSerializer
	{
		public T? Deserialize<T>(Stream stream) => default;
		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) => default;
		public void Serialize<T>(T data, Stream stream) { }
		public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default) => default;
	}
}
