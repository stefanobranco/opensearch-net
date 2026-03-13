using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class HttpClientTransportTests : IDisposable
{
	private readonly List<HttpClientTransport> _transports = [];

	public void Dispose()
	{
		foreach (var t in _transports)
			t.Dispose();
	}

	[Fact]
	public void SuccessfulRequest_DeserializesResponse()
	{
		var responseJson = JsonSerializer.Serialize(new { status = "ok" });
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (statusCode, contentType, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.Status.Should().Be("ok");
		handler.Requests.Should().HaveCount(1);
		handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/_cluster/health");
	}

	[Fact]
	public void RetriesOn503_MarksNodeDead_SelectsDifferentNode()
	{
		var callCount = 0;
		var handler = new MockHttpMessageHandler(req =>
		{
			callCount++;
			if (callCount == 1)
				return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			};
		});

		var transport = CreateTransport(handler,
			new Uri("http://node1:9200"),
			new Uri("http://node2:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.Status.Should().Be("ok");
		handler.Requests.Should().HaveCount(2);

		// The second request should go to a different node than the first.
		var firstHost = handler.Requests[0].RequestUri!.Host;
		var secondHost = handler.Requests[1].RequestUri!.Host;
		firstHost.Should().NotBe(secondHost);
	}

	[Fact]
	public void BasicAuth_AddsAuthorizationHeader()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});

		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.Authentication(new BasicAuthCredentials("admin", "password123"))
			.Build();

		var transport = CreateTransport(handler, config);

		var endpoint = CreateSimpleEndpoint();
		transport.PerformRequest("request", endpoint);

		handler.Requests.Should().HaveCount(1);
		var authHeader = handler.Requests[0].Headers.Authorization;
		authHeader.Should().NotBeNull();
		authHeader!.Scheme.Should().Be("Basic");

		var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter!));
		decoded.Should().Be("admin:password123");
	}

	[Fact]
	public void ApiKeyAuth_AddsAuthorizationHeader()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});

		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.Authentication(new ApiKeyCredentials("my-id", "my-api-key"))
			.Build();

		var transport = CreateTransport(handler, config);

		var endpoint = CreateSimpleEndpoint();
		transport.PerformRequest("request", endpoint);

		handler.Requests.Should().HaveCount(1);
		var authHeader = handler.Requests[0].Headers.Authorization;
		authHeader.Should().NotBeNull();
		authHeader!.Scheme.Should().Be("ApiKey");

		var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter!));
		decoded.Should().Be("my-id:my-api-key");
	}

	[Fact]
	public void MaxRetriesExhausted_ThrowsTransportException()
	{
		var handler = new MockHttpMessageHandler(_ =>
			throw new HttpRequestException("Connection refused"));

		var transport = CreateTransport(handler,
			new Uri("http://node1:9200"),
			new Uri("http://node2:9200"),
			new Uri("http://node3:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var act = () => transport.PerformRequest("request", endpoint);

		act.Should().Throw<TransportException>()
			.WithMessage("*Maximum number of retries*");

		// With 3 nodes, maxRetries = 2, so we should see 3 total requests (initial + 2 retries).
		handler.Requests.Should().HaveCount(3);
	}

	[Fact]
	public void Compression_AddsAcceptEncodingGzip()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});

		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.EnableHttpCompression()
			.Build();

		var transport = CreateTransport(handler, config);

		var endpoint = CreateSimpleEndpoint();
		transport.PerformRequest("request", endpoint);

		handler.Requests.Should().HaveCount(1);
		handler.Requests[0].Headers.AcceptEncoding
			.Should().Contain(e => e.Value == "gzip");
	}

	[Fact]
	public void NoCompression_DoesNotAddAcceptEncodingGzip()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = CreateSimpleEndpoint();
		transport.PerformRequest("request", endpoint);

		handler.Requests.Should().HaveCount(1);
		handler.Requests[0].Headers.AcceptEncoding.Should().BeEmpty();
	}

	[Fact]
	public async Task AsyncRequest_DeserializesResponse()
	{
		var responseJson = JsonSerializer.Serialize(new { status = "healthy" });
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = await transport.PerformRequestAsync("request", endpoint);

		response.Status.Should().Be("healthy");
	}

	[Fact]
	public async Task AsyncMaxRetriesExhausted_ThrowsTransportException()
	{
		var handler = new MockHttpMessageHandler(_ =>
			throw new HttpRequestException("Connection refused"));

		var transport = CreateTransport(handler,
			new Uri("http://node1:9200"),
			new Uri("http://node2:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var act = () => transport.PerformRequestAsync("request", endpoint);

		await act.Should().ThrowAsync<TransportException>()
			.WithMessage("*Maximum number of retries*");
	}

	[Fact]
	public void RequestWithBody_SetsContentType()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/my-index/_doc",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse(),
			contentType: "application/json",
			getBody: _ => RequestBody.Json(new { title = "test" }));

		transport.PerformRequest("request", endpoint);

		handler.Requests.Should().HaveCount(1);
		handler.Requests[0].Content.Should().NotBeNull();
		handler.Requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
	}

	[Fact]
	public void TransportException_ContainsAuditTrail()
	{
		var handler = new MockHttpMessageHandler(_ =>
			throw new HttpRequestException("Connection refused"));

		// Use 2 nodes (maxRetries = 1), both throw, so retries exhaust.
		var transport = CreateTransport(handler,
			new Uri("http://node1:9200"),
			new Uri("http://node2:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		TransportException? caught = null;
		try
		{
			transport.PerformRequest("request", endpoint);
		}
		catch (TransportException ex)
		{
			caught = ex;
		}

		caught.Should().NotBeNull();
		caught!.AuditTrail.Should().NotBeNull();
		caught.AuditTrail!.Events.Should().NotBeEmpty();
	}

	// --- Helpers ---

	private HttpClientTransport CreateTransport(MockHttpMessageHandler handler, params Uri[] uris)
	{
		var config = TransportConfiguration.Create(uris).Build();
		return CreateTransport(handler, config);
	}

	private HttpClientTransport CreateTransport(MockHttpMessageHandler handler, TransportConfiguration config)
	{
		var transport = new HttpClientTransport(config);

		// Replace the internal HttpClientFactory with one that uses our mock handler.
		// This uses reflection because HttpClientFactory is internal and the field is private.
		var factoryField = typeof(HttpClientTransport)
			.GetField("_httpClientFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;

		// Dispose the original factory to clean up its real SocketsHttpHandler.
		var originalFactory = (IDisposable)factoryField.GetValue(transport)!;
		originalFactory.Dispose();

		// Create a new HttpClientFactory backed by our mock handler.
		var mockFactory = new HttpClientFactory(
			TimeSpan.FromHours(1), // Don't rotate during tests.
			() => handler);

		factoryField.SetValue(transport, mockFactory);

		_transports.Add(transport);
		return transport;
	}

	private static SimpleEndpoint<string, TestResponse> CreateSimpleEndpoint() =>
		new(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

	public class TestResponse
	{
		public string? Status { get; set; }
	}
}

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
	public List<HttpRequestMessage> Requests { get; } = [];

	public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
		_handler = handler;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		Requests.Add(request);
		return Task.FromResult(_handler(request));
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken ct)
	{
		Requests.Add(request);
		return _handler(request);
	}
}
