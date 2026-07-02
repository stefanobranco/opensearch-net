using System.Net.Http.Headers;
using System.Text;

namespace OpenSearch.IntegrationTests.Infrastructure;

/// <summary>
/// Connection details for a security-enabled OpenSearch cluster (HTTPS, basic auth, self-signed
/// demo certificates). Reads OPENSEARCH_SECURED_URL and OPENSEARCH_ADMIN_PASSWORD from the
/// environment; the defaults match the docker-compose "secured" profile.
/// </summary>
public static class SecuredOpenSearchCluster
{
	public static Uri Url { get; } =
		new(Environment.GetEnvironmentVariable("OPENSEARCH_SECURED_URL") ?? "https://localhost:9201");

	public static string Username => "admin";

	public static string Password { get; } =
		Environment.GetEnvironmentVariable("OPENSEARCH_ADMIN_PASSWORD") ?? "IntegTest#2026";

	/// <summary>
	/// Returns true if the secured cluster is reachable with the admin credentials.
	/// </summary>
	public static bool IsAvailable()
	{
		try
		{
			using var handler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
			};
			using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
			http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
				"Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));
			var response = http.GetAsync(Url).GetAwaiter().GetResult();
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
