using OpenSearch.Net;

namespace OpenSearch.Client.Core;

/// <summary>
/// Endpoint for the bulk API. Serializes operations as NDJSON.
/// </summary>
public sealed class BulkEndpoint : IEndpoint<BulkRequest, BulkResponse>
{
	public static readonly BulkEndpoint Instance = new();

	public OpenSearch.Net.HttpMethod Method(BulkRequest r) => OpenSearch.Net.HttpMethod.Post;

	public string RequestUrl(BulkRequest r)
	{
		var path = r.Index is not null
			? $"/{Uri.EscapeDataString(r.Index)}/_bulk"
			: "/_bulk";

		var queryParts = new List<string>();
		if (r.Pipeline is not null)
			queryParts.Add($"pipeline={Uri.EscapeDataString(r.Pipeline)}");
		if (r.Refresh is not null)
			queryParts.Add($"refresh={Uri.EscapeDataString(r.Refresh)}");
		if (r.Routing is not null)
			queryParts.Add($"routing={Uri.EscapeDataString(r.Routing)}");
		if (r.Timeout is not null)
			queryParts.Add($"timeout={Uri.EscapeDataString(r.Timeout)}");
		if (r.RequireAlias is not null)
			queryParts.Add($"require_alias={Uri.EscapeDataString(r.RequireAlias.Value ? "true" : "false")}");

		return queryParts.Count > 0 ? $"{path}?{string.Join("&", queryParts)}" : path;
	}

	public string? ContentType => "application/x-ndjson";

	public RequestBody? GetBody(BulkRequest r) => RequestBody.Custom(
		"application/x-ndjson",
		(stream, serializer) => NdjsonWriter.Write(stream, r.Operations, serializer),
		(stream, serializer, ct) => NdjsonWriter.WriteAsync(stream, r.Operations, serializer, ct));

	public BulkResponse DeserializeResponse(int statusCode, string? contentType, Stream body, IOpenSearchSerializer serializer) =>
		serializer.Deserialize<BulkResponse>(body)!;
}
