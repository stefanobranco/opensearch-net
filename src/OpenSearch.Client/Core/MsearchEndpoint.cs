using OpenSearch.Net;

namespace OpenSearch.Client.Core;

/// <summary>
/// Endpoint for the msearch API. Serializes search items as NDJSON (header + body pairs).
/// </summary>
public sealed class MsearchEndpoint : IEndpoint<MsearchRequest, MsearchResponse>
{
	public static readonly MsearchEndpoint Instance = new();

	public OpenSearch.Net.HttpMethod Method(MsearchRequest r) => OpenSearch.Net.HttpMethod.Post;

	public string RequestUrl(MsearchRequest r)
	{
		var path = r.Index is not null
			? $"/{Uri.EscapeDataString(r.Index)}/_msearch"
			: "/_msearch";

		var queryParts = new List<string>();
		if (r.AllowPartialResults is not null)
			queryParts.Add($"allow_partial_results={Uri.EscapeDataString(r.AllowPartialResults.Value ? "true" : "false")}");
		if (r.CcsMinimizeRoundtrips is not null)
			queryParts.Add($"ccs_minimize_roundtrips={Uri.EscapeDataString(r.CcsMinimizeRoundtrips.Value ? "true" : "false")}");
		if (r.MaxConcurrentSearches is not null)
			queryParts.Add($"max_concurrent_searches={Uri.EscapeDataString(r.MaxConcurrentSearches.ToString()!)}");
		if (r.MaxConcurrentShardRequests is not null)
			queryParts.Add($"max_concurrent_shard_requests={Uri.EscapeDataString(r.MaxConcurrentShardRequests.ToString()!)}");
		if (r.PreFilterShardSize is not null)
			queryParts.Add($"pre_filter_shard_size={Uri.EscapeDataString(r.PreFilterShardSize.ToString()!)}");
		if (r.RestTotalHitsAsInt is not null)
			queryParts.Add($"rest_total_hits_as_int={Uri.EscapeDataString(r.RestTotalHitsAsInt.Value ? "true" : "false")}");
		if (r.SearchType is not null)
			queryParts.Add($"search_type={Uri.EscapeDataString(r.SearchType)}");
		if (r.TypedKeys is not null)
			queryParts.Add($"typed_keys={Uri.EscapeDataString(r.TypedKeys.Value ? "true" : "false")}");

		return queryParts.Count > 0 ? $"{path}?{string.Join("&", queryParts)}" : path;
	}

	public string? ContentType => "application/x-ndjson";

	public RequestBody? GetBody(MsearchRequest r) => RequestBody.Custom(
		"application/x-ndjson",
		(stream, serializer) => NdjsonWriter.WriteMsearch(stream, r.Searches, serializer),
		(stream, serializer, ct) => NdjsonWriter.WriteMsearchAsync(stream, r.Searches, serializer, ct));

	public MsearchResponse DeserializeResponse(int statusCode, string? contentType, Stream body, IOpenSearchSerializer serializer) =>
		serializer.Deserialize<MsearchResponse>(body)!;
}
