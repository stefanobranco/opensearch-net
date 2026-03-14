using OpenSearch.Net;

namespace OpenSearch.Client.Core;

/// <summary>Endpoint for the msearch_template API. Serializes search template items as NDJSON (header + body pairs).</summary>
public sealed class MsearchTemplateEndpoint : IEndpoint<MsearchTemplateRequest, MsearchResponse>
{
	public static readonly MsearchTemplateEndpoint Instance = new();

	public OpenSearch.Net.HttpMethod Method(MsearchTemplateRequest r) => OpenSearch.Net.HttpMethod.Post;

	public string RequestUrl(MsearchTemplateRequest r)
	{
		var path = r.Index is not null
			? $"/{Uri.EscapeDataString(r.Index)}/_msearch/template"
			: "/_msearch/template";

		var queryParts = new List<string>();
		if (r.CcsMinimizeRoundtrips is not null)
			queryParts.Add($"ccs_minimize_roundtrips={Uri.EscapeDataString(r.CcsMinimizeRoundtrips.Value ? "true" : "false")}");
		if (r.MaxConcurrentSearches is not null)
			queryParts.Add($"max_concurrent_searches={Uri.EscapeDataString(r.MaxConcurrentSearches.ToString()!)}");
		if (r.RestTotalHitsAsInt is not null)
			queryParts.Add($"rest_total_hits_as_int={Uri.EscapeDataString(r.RestTotalHitsAsInt.Value ? "true" : "false")}");
		if (r.SearchType is not null)
			queryParts.Add($"search_type={Uri.EscapeDataString(r.SearchType)}");
		if (r.TypedKeys is not null)
			queryParts.Add($"typed_keys={Uri.EscapeDataString(r.TypedKeys.Value ? "true" : "false")}");

		return queryParts.Count > 0 ? $"{path}?{string.Join("&", queryParts)}" : path;
	}

	public string? ContentType => "application/x-ndjson";

	public RequestBody? GetBody(MsearchTemplateRequest r) => RequestBody.Custom(
		"application/x-ndjson",
		(stream, serializer) => NdjsonWriter.WriteMsearchTemplate(stream, r.SearchTemplates, serializer),
		(stream, serializer, ct) => NdjsonWriter.WriteMsearchTemplateAsync(stream, r.SearchTemplates, serializer, ct));

	public MsearchResponse DeserializeResponse(int statusCode, string? contentType, Stream body, IOpenSearchSerializer serializer) =>
		serializer.Deserialize<MsearchResponse>(body)!;
}
