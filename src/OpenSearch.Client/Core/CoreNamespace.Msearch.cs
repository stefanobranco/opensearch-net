using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

public sealed partial class CoreNamespace
{
	/// <summary>Allows to execute several search operations in one request.</summary>
	public MsearchResponse Msearch(MsearchRequest request, TransportOptions? options = null) =>
		_client.DoRequest(request, MsearchEndpoint.Instance, options);

	/// <summary>Allows to execute several search operations in one request.</summary>
	public Task<MsearchResponse> MsearchAsync(MsearchRequest request, TransportOptions? options = null, CancellationToken ct = default) =>
		_client.DoRequestAsync(request, MsearchEndpoint.Instance, options, ct);
}
