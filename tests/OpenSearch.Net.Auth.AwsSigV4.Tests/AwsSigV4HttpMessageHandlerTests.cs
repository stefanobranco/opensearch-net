using System.Net;
using System.Text;
using Amazon.Runtime;
using FluentAssertions;
using OpenSearch.Net.Auth.AwsSigV4;
using Xunit;

namespace OpenSearch.Net.Auth.AwsSigV4.Tests;

public class AwsSigV4HttpMessageHandlerTests
{
	private static readonly BasicAWSCredentials TestCredentials =
		new("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");

	[Fact]
	public async Task SignedRequest_HasAuthorizationHeader()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/");

		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		var authHeader = authValues!.First();
		authHeader.Should().StartWith("AWS4-HMAC-SHA256 ");
	}

	[Fact]
	public async Task SignedRequest_HasDateHeader()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/");

		capturedRequest!.Headers.TryGetValues("x-amz-date", out var dateValues).Should().BeTrue();
		dateValues!.First().Should().MatchRegex(@"^\d{8}T\d{6}Z$");
	}

	[Fact]
	public async Task SignedRequest_HasContentSha256Header()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/");

		capturedRequest!.Headers.TryGetValues("x-amz-content-sha256", out var values).Should().BeTrue();
		values!.First().Should().HaveLength(64); // SHA-256 hex string
	}

	[Fact]
	public async Task SignedRequest_WithSessionToken_HasSecurityTokenHeader()
	{
		var sessionCredentials = new SessionAWSCredentials("AKID", "SECRET", "SESSION_TOKEN");
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(sessionCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/");

		capturedRequest!.Headers.TryGetValues("x-amz-security-token", out var values).Should().BeTrue();
		values!.First().Should().Be("SESSION_TOKEN");
	}

	[Fact]
	public async Task SignedRequest_WithBody_SignsBody()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);

		var content = new StringContent("""{"query":{"match_all":{}}}""", Encoding.UTF8, "application/json");
		await client.PostAsync("https://search-domain.us-east-1.es.amazonaws.com/_search", content);

		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		authValues!.First().Should().StartWith("AWS4-HMAC-SHA256 ");

		// Body hash should NOT be the empty-body hash.
		capturedRequest.Headers.TryGetValues("x-amz-content-sha256", out var values).Should().BeTrue();
		values!.First().Should()
			.NotBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
	}

	[Fact]
	public async Task SignedRequest_AuthorizationContainsCredentialAndSignedHeaders()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/");

		var authValue = capturedRequest!.Headers.GetValues("Authorization").First();
		authValue.Should().Contain("Credential=AKIAIOSFODNN7EXAMPLE/");
		authValue.Should().Contain("SignedHeaders=");
		authValue.Should().Contain("Signature=");
	}

	[Fact]
	public async Task SignedRequest_WithQueryString_SortsParameters()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://search-domain.us-east-1.es.amazonaws.com/_search?size=10&from=0");

		// The request should still be signed successfully.
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		authValues!.First().Should().StartWith("AWS4-HMAC-SHA256 ");
	}

	[Fact]
	public async Task SignedRequest_UsesCorrectServiceName()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		// Use "aoss" for OpenSearch Serverless.
		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-west-2", "aoss", inner);
		using var client = new HttpClient(handler);
		await client.GetAsync("https://collection.us-west-2.aoss.amazonaws.com/");

		var authValue = capturedRequest!.Headers.GetValues("Authorization").First();
		authValue.Should().Contain("/us-west-2/aoss/aws4_request");
	}

	[Fact]
	public void Constructor_NullCredentials_Throws()
	{
		var act = () => new AwsSigV4HttpMessageHandler(null!, "us-east-1");
		act.Should().Throw<ArgumentNullException>().WithParameterName("credentials");
	}

	[Fact]
	public void Constructor_NullRegion_Throws()
	{
		var act = () => new AwsSigV4HttpMessageHandler(TestCredentials, null!);
		act.Should().Throw<ArgumentNullException>().WithParameterName("region");
	}

	[Fact]
	public async Task SignedRequest_PreservesContentType()
	{
		HttpRequestMessage? capturedRequest = null;
		var inner = new TestHandler(req =>
		{
			capturedRequest = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(TestCredentials, "us-east-1", "es", inner);
		using var client = new HttpClient(handler);

		var content = new StringContent("""{"query":{"match_all":{}}}""", Encoding.UTF8, "application/json");
		await client.PostAsync("https://search-domain.us-east-1.es.amazonaws.com/_search", content);

		capturedRequest!.Content.Should().NotBeNull();
		capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
	}

	private sealed class TestHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

		public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
			_handler = handler;

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken ct) =>
			Task.FromResult(_handler(request));

		protected override HttpResponseMessage Send(
			HttpRequestMessage request, CancellationToken ct) =>
			_handler(request);
	}
}
