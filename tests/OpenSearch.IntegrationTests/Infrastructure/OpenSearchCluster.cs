namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Manages the connection to a running OpenSearch cluster for integration tests.
/// Reads OPENSEARCH_URL from environment, defaults to http://localhost:9200.
/// </summary>
public static class OpenSearchCluster
{
	public static Uri Url { get; } =
		new(Environment.GetEnvironmentVariable("OPENSEARCH_URL") ?? "http://localhost:9200");

	/// <summary>
	/// Returns true if the cluster is reachable.
	/// </summary>
	public static bool IsAvailable()
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
			var response = http.GetAsync(Url).GetAwaiter().GetResult();
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
