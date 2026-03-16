using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

public sealed partial class CoreNamespace
{
	/// <summary>Allows to execute several search template operations in one request.</summary>
	public MsearchResponse MsearchTemplate(MsearchTemplateRequest request, TransportOptions? options = null) =>
		_client.DoRequest(request, MsearchTemplateEndpoint.Instance, options);

	/// <summary>Allows to execute several search template operations in one request.</summary>
	public Task<MsearchResponse> MsearchTemplateAsync(MsearchTemplateRequest request, TransportOptions? options = null, CancellationToken ct = default) =>
		_client.DoRequestAsync(request, MsearchTemplateEndpoint.Instance, options, ct);
}
