using System.Net;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenSearch.Net;
using Xunit;

namespace OpenSearch.Net.Tests;

public class ApiCallDetailsTests : IDisposable
{
	private readonly List<HttpClientTransport> _transports = [];

	public void Dispose()
	{
		foreach (var t in _transports)
			t.Dispose();
	}

	[Fact]
	public void SuccessfulRequest_AttachesApiCallDetails()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = transport.PerformRequest("request", endpoint);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.HttpMethod.Should().Be(HttpMethod.Get);
		details.Uri!.AbsolutePath.Should().Be("/_cluster/health");
		details.HttpStatusCode.Should().Be(200);
		details.Success.Should().BeTrue();
		details.Duration.Should().BeGreaterThan(TimeSpan.Zero);
		details.Node.Should().NotBeNull();
		details.Node!.Host.Should().Be(new Uri("http://localhost:9200/"));
		details.AuditTrail.Should().NotBeNull();
		details.AuditTrail!.Events.Should().NotBeEmpty();
		details.OriginalException.Should().BeNull();
	}

	[Fact]
	public async Task AsyncRequest_AttachesApiCallDetails()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/my-index/_search",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = await transport.PerformRequestAsync("request", endpoint);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.HttpMethod.Should().Be(HttpMethod.Post);
		details.Uri!.AbsolutePath.Should().Be("/my-index/_search");
		details.HttpStatusCode.Should().Be(200);
		details.Success.Should().BeTrue();
	}

	[Fact]
	public void DisableDirectStreaming_CapturesRequestBody()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.DisableDirectStreaming()
			.Build();

		var transport = CreateTransport(handler, config);

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/my-index/_search",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse(),
			getBody: _ => RequestBody.Json(new { query = new { match_all = new { } } }));

		var response = transport.PerformRequest("request", endpoint);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.RequestBodyBytes.Should().NotBeNull();
		var requestJson = Encoding.UTF8.GetString(details.RequestBodyBytes!);
		requestJson.Should().Contain("match_all");
	}

	[Fact]
	public void DisableDirectStreaming_CapturesResponseBody()
	{
		var responseJson = "{\"status\":\"ok\",\"took\":5}";
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.DisableDirectStreaming()
			.Build();

		var transport = CreateTransport(handler, config);

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_cluster/health",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse());

		var response = transport.PerformRequest("request", endpoint);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.ResponseBodyBytes.Should().NotBeNull();
		var capturedJson = Encoding.UTF8.GetString(details.ResponseBodyBytes!);
		capturedJson.Should().Be(responseJson);
	}

	[Fact]
	public void DirectStreamingEnabled_DoesNotCaptureBodyByDefault()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/my-index/_search",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse(),
			getBody: _ => RequestBody.Json(new { query = new { match_all = new { } } }));

		var response = transport.PerformRequest("request", endpoint);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.RequestBodyBytes.Should().BeNull();
		details.ResponseBodyBytes.Should().BeNull();
	}

	[Fact]
	public void PerRequestOverride_DisableDirectStreaming()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
			});

		// Global: streaming enabled (default)
		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestResponse>(body) ?? new TestResponse(),
			getBody: _ => RequestBody.Json(new { key = "value" }));

		// Per-request: enable body capture
		var options = new TransportOptions { DisableDirectStreaming = true };
		var response = transport.PerformRequest("request", endpoint, options);

		var details = response.GetApiCallDetails();
		details.Should().NotBeNull();
		details!.RequestBodyBytes.Should().NotBeNull();
		details.ResponseBodyBytes.Should().NotBeNull();
	}

	[Fact]
	public void GetApiCallDetails_ReturnsNullForNonOpenSearchResponse()
	{
		var response = new TestResponse { Status = "test" };
		var retrieved = response.GetApiCallDetails();
		retrieved.Should().BeNull();
	}

	[Fact]
	public void GetApiCallDetails_ReturnsApiCall_ForOpenSearchResponse()
	{
		var details = new ApiCallDetails
		{
			HttpMethod = HttpMethod.Get,
			HttpStatusCode = 200,
			Success = true,
			Duration = TimeSpan.FromMilliseconds(42)
		};

		var response = new TestOsResponse { ApiCall = details };
		var retrieved = response.GetApiCallDetails();

		retrieved.Should().BeSameAs(details);
	}

	[Fact]
	public void DebugInformation_FormatsCorrectly()
	{
		var trail = new RequestAuditTrail();
		trail.Add(AuditEventType.NodeSelected, new Node(new Uri("http://localhost:9200")));
		trail.Add(AuditEventType.RequestSent, new Node(new Uri("http://localhost:9200")));
		trail.Add(AuditEventType.ResponseReceived, new Node(new Uri("http://localhost:9200")),
			statusCode: 200, duration: TimeSpan.FromMilliseconds(147));

		var details = new ApiCallDetails
		{
			HttpMethod = HttpMethod.Post,
			Uri = new Uri("http://localhost:9200/my-index/_search"),
			Node = new Node(new Uri("http://localhost:9200")),
			HttpStatusCode = 200,
			Success = true,
			Duration = TimeSpan.FromMilliseconds(150),
			AuditTrail = trail,
			RequestBodyBytes = Encoding.UTF8.GetBytes("{\"query\":{\"match_all\":{}}}"),
			ResponseBodyBytes = Encoding.UTF8.GetBytes("{\"took\":5,\"timed_out\":false}")
		};

		var debugInfo = details.DebugInformation();

		debugInfo.Should().Contain("Valid OpenSearch response built from a successful call on POST:");
		debugInfo.Should().Contain("/my-index/_search");
		debugInfo.Should().Contain("# Audit trail:");
		debugInfo.Should().Contain("NodeSelected:");
		debugInfo.Should().Contain("RequestSent:");
		debugInfo.Should().Contain("ResponseReceived:");
		debugInfo.Should().Contain("Status: 200");
		debugInfo.Should().Contain("# Request:");
		debugInfo.Should().Contain("match_all");
		debugInfo.Should().Contain("# Response:");
		debugInfo.Should().Contain("timed_out");
	}

	[Fact]
	public void DebugInformation_UnsuccessfulCall()
	{
		var details = new ApiCallDetails
		{
			HttpMethod = HttpMethod.Get,
			Uri = new Uri("http://localhost:9200/_cluster/health"),
			HttpStatusCode = 503,
			Success = false,
			Duration = TimeSpan.FromMilliseconds(50)
		};

		var debugInfo = details.DebugInformation();

		debugInfo.Should().Contain("Invalid OpenSearch response built from a unsuccessful call on GET:");
		debugInfo.Should().Contain("/_cluster/health");
	}

	[Fact]
	public void TransportException_CarriesApiCallDetails()
	{
		var handler = new MockHttpMessageHandler(_ =>
			throw new HttpRequestException("Connection refused"));

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
		caught!.ApiCallDetails.Should().NotBeNull();
		caught.ApiCallDetails!.Success.Should().BeFalse();
		caught.ApiCallDetails.AuditTrail.Should().NotBeNull();
	}

	[Fact]
	public void DisableDirectStreaming_ConfigurationProperty()
	{
		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.DisableDirectStreaming()
			.Build();

		config.DisableDirectStreaming.Should().BeTrue();
	}

	[Fact]
	public void DisableDirectStreaming_DefaultIsFalse()
	{
		var config = TransportConfiguration.Create(new Uri("http://localhost:9200"))
			.Build();

		config.DisableDirectStreaming.Should().BeFalse();
	}

	[Fact]
	public void OpenSearchResponse_BaseClass_PopulatedOnSuccess()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{\"value\":\"test\"}", Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestOsResponse>(
			method: _ => HttpMethod.Get,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOsResponse>(body) ?? new TestOsResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.IsValid.Should().BeTrue();
		response.ApiCall.Should().NotBeNull();
		response.ApiCall!.HttpStatusCode.Should().Be(200);
		response.ServerError.Should().BeNull();
		response.OriginalException.Should().BeNull();
		response.DebugInformation.Should().Contain("Valid");
	}

	[Fact]
	public void OpenSearchResponse_BaseClass_PopulatedOnError()
	{
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(
					"{\"error\":{\"type\":\"parsing_exception\",\"reason\":\"Bad query\"},\"status\":400}",
					Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestOsResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOsResponse>(body) ?? new TestOsResponse());

		var response = transport.PerformRequest("request", endpoint);

		response.IsValid.Should().BeFalse();
		response.ApiCall.Should().NotBeNull();
		response.ApiCall!.HttpStatusCode.Should().Be(400);
		response.ServerError.Should().NotBeNull();
		response.ServerError!.Status.Should().Be(400);
		response.ServerError.Error!.Type.Should().Be("parsing_exception");
		response.ServerError.Error.Reason.Should().Be("Bad query");
		response.ServerError.ToString().Should().Contain("400");
		response.DebugInformation.Should().Contain("Invalid");
	}

	[Fact]
	public void ServerError_ErrorBody_CapturedInResponseBytes()
	{
		var errorJson = "{\"error\":{\"type\":\"parsing_exception\",\"reason\":\"Bad query\"},\"status\":400}";
		var handler = new MockHttpMessageHandler(_ =>
			new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
			});

		var transport = CreateTransport(handler, new Uri("http://localhost:9200"));

		var endpoint = new SimpleEndpoint<string, TestOsResponse>(
			method: _ => HttpMethod.Post,
			requestUrl: _ => "/_test",
			deserialize: (_, _, body, serializer) =>
				serializer.Deserialize<TestOsResponse>(body) ?? new TestOsResponse());

		var response = transport.PerformRequest("request", endpoint);

		// Error body bytes are always captured (for debug info)
		response.ApiCall!.ResponseBodyBytes.Should().NotBeNull();
		var captured = Encoding.UTF8.GetString(response.ApiCall.ResponseBodyBytes!);
		captured.Should().Contain("parsing_exception");
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

		var factoryField = typeof(HttpClientTransport)
			.GetField("_httpClientFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;

		var originalFactory = (IDisposable)factoryField.GetValue(transport)!;
		originalFactory.Dispose();

		var mockFactory = new HttpClientFactory(
			TimeSpan.FromHours(1),
			() => handler);

		factoryField.SetValue(transport, mockFactory);

		_transports.Add(transport);
		return transport;
	}

	public class TestResponse : OpenSearchResponse
	{
		public string? Status { get; set; }
	}

	public class TestOsResponse : OpenSearchResponse
	{
		public string? Value { get; set; }
	}
}
