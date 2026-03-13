namespace OpenSearch.Net;

/// <summary>
/// API key authentication credentials.
/// </summary>
public sealed record ApiKeyCredentials(string Id, string ApiKey);
