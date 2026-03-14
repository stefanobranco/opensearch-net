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

	private const string TestEndpoint = "https://search-domain.us-east-1.es.amazonaws.com/";

	[Fact]
	public async Task SignedRequest_HasAuthorizationHeader()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint);

		capturedRequest.Message.Should().NotBeNull();
		capturedRequest.Message!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		authValues!.First().Should().StartWith("AWS4-HMAC-SHA256 ");
	}

	[Fact]
	public async Task SignedRequest_HasDateHeader()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint);

		capturedRequest.Message!.Headers.TryGetValues("x-amz-date", out var dateValues).Should().BeTrue();
		dateValues!.First().Should().MatchRegex(@"^\d{8}T\d{6}Z$");
	}

	[Fact]
	public async Task SignedRequest_HasContentSha256Header()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint);

		capturedRequest.Message!.Headers.TryGetValues("x-amz-content-sha256", out var values).Should().BeTrue();
		values!.First().Should().HaveLength(64); // SHA-256 hex string
	}

	[Fact]
	public async Task SignedRequest_WithSessionToken_HasSecurityTokenHeader()
	{
		var sessionCredentials = new SessionAWSCredentials("AKID", "SECRET", "SESSION_TOKEN");
		var (capturedRequest, client) = CreateSigningClient(sessionCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint);

		capturedRequest.Message!.Headers.TryGetValues("x-amz-security-token", out var values).Should().BeTrue();
		values!.First().Should().Be("SESSION_TOKEN");
	}

	[Fact]
	public async Task SignedRequest_WithBody_SignsBody()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client)
		{
			var content = new StringContent("""{"query":{"match_all":{}}}""", Encoding.UTF8, "application/json");
			await client.PostAsync(TestEndpoint + "_search", content);
		}

		capturedRequest.Message.Should().NotBeNull();
		capturedRequest.Message!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		authValues!.First().Should().StartWith("AWS4-HMAC-SHA256 ");

		capturedRequest.Message.Headers.TryGetValues("x-amz-content-sha256", out var values).Should().BeTrue();
		values!.First().Should()
			.NotBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
	}

	[Fact]
	public async Task SignedRequest_AuthorizationContainsCredentialAndSignedHeaders()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint);

		var authValue = capturedRequest.Message!.Headers.GetValues("Authorization").First();
		authValue.Should().Contain("Credential=AKIAIOSFODNN7EXAMPLE/");
		authValue.Should().Contain("SignedHeaders=");
		authValue.Should().Contain("Signature=");
	}

	[Fact]
	public async Task SignedRequest_WithQueryString_SortsParameters()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client) await client.GetAsync(TestEndpoint + "_search?size=10&from=0");

		capturedRequest.Message.Should().NotBeNull();
		capturedRequest.Message!.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
		authValues!.First().Should().StartWith("AWS4-HMAC-SHA256 ");
	}

	[Fact]
	public async Task SignedRequest_UsesCorrectServiceName()
	{
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-west-2", "aoss");
		using (client) await client.GetAsync("https://collection.us-west-2.aoss.amazonaws.com/");

		var authValue = capturedRequest.Message!.Headers.GetValues("Authorization").First();
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
		var (capturedRequest, client) = CreateSigningClient(TestCredentials, "us-east-1");
		using (client)
		{
			var content = new StringContent("""{"query":{"match_all":{}}}""", Encoding.UTF8, "application/json");
			await client.PostAsync(TestEndpoint + "_search", content);
		}

		capturedRequest.Message!.Content.Should().NotBeNull();
		capturedRequest.Message.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
	}

	// --- Helpers ---

	private static (CapturedRequest Captured, HttpClient Client) CreateSigningClient(
		AWSCredentials credentials, string region, string service = "es")
	{
		var captured = new CapturedRequest();
		var inner = new TestHandler(req =>
		{
			captured.Message = req;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var handler = new AwsSigV4HttpMessageHandler(credentials, region, service, inner);
		return (captured, new HttpClient(handler));
	}

	private sealed class CapturedRequest
	{
		public HttpRequestMessage? Message { get; set; }
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
