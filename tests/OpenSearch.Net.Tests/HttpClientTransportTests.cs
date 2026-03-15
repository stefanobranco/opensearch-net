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

	[Fact]
	public void Returns400_ReturnsResponseWithServerError()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"parsing_exception\",\"reason\":\"Unknown key for a VALUE_STRING in [invalid]\"},\"status\":400}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = CreateSimpleEndpoint();
		var response = transport.PerformRequest("request", endpoint);

		response.GetApiCallDetails().Should().NotBeNull();
		response.GetApiCallDetails()!.HttpStatusCode.Should().Be(400);
		response.GetApiCallDetails()!.Success.Should().BeFalse();
		handler.Requests.Should().HaveCount(1); // No retry
	}

	[Fact]
	public void Returns400_WithThrowExceptions_ThrowsServerException()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"parsing_exception\",\"reason\":\"Unknown key for a VALUE_STRING in [invalid]\"},\"status\":400}",
					Encoding.UTF8, "application/json")
			});
		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.ThrowExceptions()
			.Build();
		var transport = CreateTransport(handler, config);
		var endpoint = CreateSimpleEndpoint();
		var act = () => transport.PerformRequest("request", endpoint);
		act.Should().Throw<OpenSearchServerException>()
			.Where(e => e.StatusCode == 400 && e.ErrorType == "parsing_exception");
		handler.Requests.Should().HaveCount(1); // No retry
	}

	[Fact]
	public void Returns401_ReturnsResponseWithServerError_NoRetry()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.Unauthorized)
			{
				Content = new StringContent(
					"{\"error\":\"Security exception\",\"status\":401}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://node1:9200"), new Uri("http://node2:9200"));
		var endpoint = CreateSimpleEndpoint();
		var response = transport.PerformRequest("request", endpoint);

		response.GetApiCallDetails().Should().NotBeNull();
		response.GetApiCallDetails()!.HttpStatusCode.Should().Be(401);
		handler.Requests.Should().HaveCount(1);
	}

	[Fact]
	public void Returns404_OnGetEndpoint_DoesNotThrow()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent("{\"found\":false}", Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/my-index/_doc/1",
			deserialize: (statusCode, _, body, serializer) =>
				new TestResponse { Status = statusCode == 404 ? "not_found" : "ok" });
		var response = transport.PerformRequest("request", endpoint);
		response.Status.Should().Be("not_found");
	}

	[Fact]
	public void Returns404_OnPostEndpoint_ReturnsResponseWithServerError()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"index_not_found_exception\",\"reason\":\"no such index [missing]\"},\"status\":404}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/missing/_search",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());
		var response = transport.PerformRequest("request", endpoint);
		response.GetApiCallDetails().Should().NotBeNull();
		response.GetApiCallDetails()!.HttpStatusCode.Should().Be(404);
	}

	[Fact]
	public void Returns409_ReturnsResponseWithServerError_NoRetry()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.Conflict)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"version_conflict_engine_exception\",\"reason\":\"conflict\"},\"status\":409}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://node1:9200"), new Uri("http://node2:9200"));
		var endpoint = CreateSimpleEndpoint();
		var response = transport.PerformRequest("request", endpoint);
		response.GetApiCallDetails().Should().NotBeNull();
		response.GetApiCallDetails()!.HttpStatusCode.Should().Be(409);
		handler.Requests.Should().HaveCount(1);
	}

	[Fact]
	public void Returns502_Retries()
	{
		var callCount = 0;
		var handler = new MockHttpMessageHandler(_ =>
		{
			callCount++;
			if (callCount == 1)
				return new HttpResponseMessage(HttpStatusCode.BadGateway);
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			};
		});
		var transport = CreateTransport(handler, new Uri("http://node1:9200"), new Uri("http://node2:9200"));
		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());
		var response = transport.PerformRequest("request", endpoint);
		response.Status.Should().Be("ok");
		handler.Requests.Should().HaveCount(2);
	}

	[Fact]
	public void Returns504_Retries()
	{
		var callCount = 0;
		var handler = new MockHttpMessageHandler(_ =>
		{
			callCount++;
			if (callCount == 1)
				return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			};
		});
		var transport = CreateTransport(handler, new Uri("http://node1:9200"), new Uri("http://node2:9200"));
		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());
		var response = transport.PerformRequest("request", endpoint);
		response.Status.Should().Be("ok");
		handler.Requests.Should().HaveCount(2);
	}

	[Fact]
	public async Task CancellationToken_Cancelled_ThrowsOperationCanceledException()
	{
		var handler = new MockHttpMessageHandler(_ =>
		{
			throw new TaskCanceledException("Cancelled", new Exception(), new CancellationToken(true));
		});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = CreateSimpleEndpoint();
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		// TaskCanceledException from user cancellation should not be caught by the retry logic
		// when ct.IsCancellationRequested is true
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => transport.PerformRequestAsync("request", endpoint, ct: cts.Token));
	}

	[Fact]
	public void ConnectionRefused_Retries_ThenThrows()
	{
		var handler = new MockHttpMessageHandler(_ =>
			throw new HttpRequestException("Connection refused"));
		var config = TransportConfiguration.Create(
				new Uri("http://node1:9200"),
				new Uri("http://node2:9200"),
				new Uri("http://node3:9200"))
			.MaxRetries(2)
			.Build();
		var transport = CreateTransport(handler, config);
		var endpoint = CreateSimpleEndpoint();
		var act = () => transport.PerformRequest("request", endpoint);
		act.Should().Throw<TransportException>()
			.WithMessage("*Maximum number of retries*");
		handler.Requests.Should().HaveCount(3);
	}

	[Fact]
	public void Headers_FromTransportOptions_SentWithRequest()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = CreateSimpleEndpoint();
		var options = new TransportOptions
		{
			Headers = new Dictionary<string, string> { ["X-Custom-Header"] = "test-value" }
		};
		transport.PerformRequest("request", endpoint, options);
		handler.Requests.Should().HaveCount(1);
		handler.Requests[0].Headers.GetValues("X-Custom-Header").Should().ContainSingle("test-value");
	}

	[Fact]
	public void HeadRequest_Returns404_DoesNotThrow()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.NotFound));
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Head,
			requestUrl: _ => "/my-index",
			deserialize: (statusCode, _, _, _) =>
				new TestResponse { Status = statusCode is >= 200 and < 300 ? "exists" : "not_exists" });
		var response = transport.PerformRequest("request", endpoint);
		response.Status.Should().Be("not_exists");
	}

	// ── OpenSearchResponse base class tests ──

	[Fact]
	public void ServerError_PopulatedOnBaseClass_WhenResponseInherits()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"parsing_exception\",\"reason\":\"Bad query\",\"root_cause\":[{\"type\":\"parsing_exception\",\"reason\":\"Bad query\"}]},\"status\":400}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestOpenSearchResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/test/_search",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOpenSearchResponse>(body) ?? new TestOpenSearchResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.IsValid.Should().BeFalse();
		response.ServerError.Should().NotBeNull();
		response.ServerError!.Status.Should().Be(400);
		response.ServerError.Error.Should().NotBeNull();
		response.ServerError.Error!.Type.Should().Be("parsing_exception");
		response.ServerError.Error.Reason.Should().Be("Bad query");
		response.ServerError.Error.RootCause.Should().NotBeNull();
		response.ServerError.Error.RootCause.Should().HaveCount(1);
		response.ApiCall.Should().NotBeNull();
		response.ApiCall!.HttpStatusCode.Should().Be(400);
		response.ApiCall.Success.Should().BeFalse();
		response.OriginalException.Should().BeNull();
		response.DebugInformation.Should().Contain("Invalid OpenSearch response");
	}

	[Fact]
	public void SuccessfulRequest_PopulatesBaseClass()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"value\":\"hello\"}", Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestOpenSearchResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOpenSearchResponse>(body) ?? new TestOpenSearchResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.IsValid.Should().BeTrue();
		response.ServerError.Should().BeNull();
		response.ApiCall.Should().NotBeNull();
		response.ApiCall!.HttpStatusCode.Should().Be(200);
		response.ApiCall.Success.Should().BeTrue();
	}

	[Fact]
	public void ServerError_StringError_ParsedCorrectly()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.Unauthorized)
			{
				Content = new StringContent(
					"{\"error\":\"Security exception\",\"status\":401}",
					Encoding.UTF8, "application/json")
			});
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));
		var endpoint = new SimpleEndpoint<string, TestOpenSearchResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOpenSearchResponse>(body) ?? new TestOpenSearchResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.IsValid.Should().BeFalse();
		response.ServerError.Should().NotBeNull();
		response.ServerError!.Status.Should().Be(401);
		response.ServerError.Error.Should().NotBeNull();
		response.ServerError.Error!.Reason.Should().Be("Security exception");
		response.ServerError.Error.Type.Should().BeNull();
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

	public class TestOpenSearchResponse : OpenSearchResponse
	{
		public string? Value { get; set; }
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
