using System.Net;
using System.Net.Http.Headers;

namespace OpenSearch.Benchmarks;

/// <summary>
/// Terminal HTTP handler that answers every request with a canned JSON body, so benchmarks
/// exercise the full transport pipeline without a network. The request content is drained
/// first — body serialization only happens when something consumes the content stream.
/// </summary>
public sealed class CannedResponseHandler : HttpMessageHandler
{
	private readonly byte[] _responseBody;

	public CannedResponseHandler(byte[] responseBody) => _responseBody = responseBody;

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken ct)
	{
		request.Content?.CopyTo(Stream.Null, null, ct);
		return CreateResponse();
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		if (request.Content is not null)
			await request.Content.CopyToAsync(Stream.Null, ct).ConfigureAwait(false);
		return CreateResponse();
	}

	private HttpResponseMessage CreateResponse()
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new ByteArrayContent(_responseBody),
		};
		response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return response;
	}
}
