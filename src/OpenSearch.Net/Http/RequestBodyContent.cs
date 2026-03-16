using System.Net;
using System.Net.Http.Headers;

namespace OpenSearch.Net;

/// <summary>
/// Custom <see cref="HttpContent"/> that writes the request body directly to the network stream,
/// avoiding an intermediate <see cref="MemoryStream"/> copy.
/// </summary>
internal sealed class RequestBodyContent : HttpContent
{
	private readonly RequestBody _body;
	private readonly IOpenSearchSerializer _serializer;

	public RequestBodyContent(RequestBody body, IOpenSearchSerializer serializer)
	{
		_body = body;
		_serializer = serializer;
		Headers.ContentType = new MediaTypeHeaderValue(body.ContentType);
	}

	protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
		_body.WriteTo(stream, _serializer);

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
		SerializeToStreamAsync(stream, context, CancellationToken.None);

	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
		await _body.WriteToAsync(stream, _serializer, cancellationToken).ConfigureAwait(false);

	protected override bool TryComputeLength(out long length)
	{
		length = 0;
		return false;
	}
}
