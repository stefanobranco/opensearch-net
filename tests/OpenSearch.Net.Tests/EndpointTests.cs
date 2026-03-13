using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class EndpointTests
{
	[Theory]
	[InlineData(HttpMethod.Get)]
	[InlineData(HttpMethod.Post)]
	[InlineData(HttpMethod.Put)]
	[InlineData(HttpMethod.Delete)]
	public void Method_ReturnsCorrectHttpMethod(HttpMethod expected)
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => expected,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result");

		endpoint.Method("request").Should().Be(expected);
	}

	[Fact]
	public void RequestUrl_ConstructsCorrectPath()
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Get,
			requestUrl: req => $"/my-index/{req}",
			deserialize: (_, _, _, _) => "result");

		endpoint.RequestUrl("_search").Should().Be("/my-index/_search");
	}

	[Fact]
	public void GetBody_ReturnsBodyWhenPresent()
	{
		var body = RequestBody.String("hello");
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result",
			contentType: "application/json",
			getBody: _ => body);

		endpoint.GetBody("request").Should().BeSameAs(body);
	}

	[Fact]
	public void GetBody_ReturnsNullWhenNoBodyDelegate()
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result");

		endpoint.GetBody("request").Should().BeNull();
	}

	[Fact]
	public void GetBody_ReturnsNullWhenDelegateReturnsNull()
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result",
			getBody: _ => null);

		endpoint.GetBody("request").Should().BeNull();
	}

	[Fact]
	public void ContentType_ReturnsConfiguredValue()
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result",
			contentType: "application/json");

		endpoint.ContentType.Should().Be("application/json");
	}

	[Fact]
	public void ContentType_ReturnsNullWhenNotConfigured()
	{
		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/test",
			deserialize: (_, _, _, _) => "result");

		endpoint.ContentType.Should().BeNull();
	}

	[Fact]
	public void DeserializeResponse_DelegatesToSerializer()
	{
		var serializerCalled = false;
		IOpenSearchSerializer? capturedSerializer = null;
		var capturedStatusCode = 0;
		string? capturedContentType = null;

		var endpoint = new SimpleEndpoint<string, string>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/test",
			deserialize: (statusCode, contentType, body, serializer) =>
			{
				serializerCalled = true;
				capturedStatusCode = statusCode;
				capturedContentType = contentType;
				capturedSerializer = serializer;
				return "deserialized";
			});

		var mockSerializer = new MockSerializer();
		using var stream = new MemoryStream();

		var result = endpoint.DeserializeResponse(200, "application/json", stream, mockSerializer);

		result.Should().Be("deserialized");
		serializerCalled.Should().BeTrue();
		capturedStatusCode.Should().Be(200);
		capturedContentType.Should().Be("application/json");
		capturedSerializer.Should().BeSameAs(mockSerializer);
	}

	private sealed class MockSerializer : IOpenSearchSerializer
	{
		public T? Deserialize<T>(Stream stream) => default;
		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken ct = default) => default;
		public void Serialize<T>(T data, Stream stream) { }
		public ValueTask SerializeAsync<T>(T data, Stream stream, CancellationToken ct = default) => default;
	}
}
