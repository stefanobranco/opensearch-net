using OpenSearch.Client.Core;
using OpenSearch.Net;

namespace OpenSearch.Client;

public sealed partial class CoreNamespace
{
	/// <summary>Allows to perform multiple index/update/delete operations in a single request.</summary>
	public BulkResponse Bulk(BulkRequest request, TransportOptions? options = null) =>
		_client.DoRequest(request, BulkEndpoint.Instance, options);

	/// <summary>Allows to perform multiple index/update/delete operations in a single request.</summary>
	public Task<BulkResponse> BulkAsync(BulkRequest request, TransportOptions? options = null, CancellationToken ct = default) =>
		_client.DoRequestAsync(request, BulkEndpoint.Instance, options, ct);
}
