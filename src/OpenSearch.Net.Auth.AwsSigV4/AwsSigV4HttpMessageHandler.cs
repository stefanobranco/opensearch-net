using System.Security.Cryptography;
using System.Text;

namespace OpenSearch.Net.Auth.AwsSigV4;

/// <summary>
/// A <see cref="DelegatingHandler"/> that signs HTTP requests using AWS Signature Version 4.
/// Wraps an inner <see cref="HttpMessageHandler"/> and adds the required
/// <c>Authorization</c>, <c>x-amz-date</c>, <c>x-amz-content-sha256</c>, and
/// (when using temporary credentials) <c>x-amz-security-token</c> headers.
/// </summary>
public sealed class AwsSigV4HttpMessageHandler : DelegatingHandler
{
	private readonly Amazon.Runtime.AWSCredentials _credentials;
	private readonly string _region;
	private readonly string _service;

	/// <summary>
	/// Creates a new <see cref="AwsSigV4HttpMessageHandler"/>.
	/// </summary>
	/// <param name="credentials">
	/// AWS credentials used to sign each request.
	/// Supports <see cref="Amazon.Runtime.BasicAWSCredentials"/>,
	/// <see cref="Amazon.Runtime.SessionAWSCredentials"/>, and any other
	/// <see cref="Amazon.Runtime.AWSCredentials"/> implementation.
	/// </param>
	/// <param name="region">The AWS region (e.g., <c>us-east-1</c>).</param>
	/// <param name="service">
	/// The AWS service name used in the credential scope.
	/// Defaults to <c>es</c> for Amazon OpenSearch Service.
	/// Use <c>aoss</c> for Amazon OpenSearch Serverless.
	/// </param>
	/// <param name="innerHandler">
	/// The inner handler to delegate to. When <c>null</c>, a new
	/// <see cref="HttpClientHandler"/> is used.
	/// </param>
	public AwsSigV4HttpMessageHandler(
		Amazon.Runtime.AWSCredentials credentials,
		string region,
		string service = "es",
		HttpMessageHandler? innerHandler = null)
		: base(innerHandler ?? new HttpClientHandler())
	{
		ArgumentNullException.ThrowIfNull(credentials);
		ArgumentNullException.ThrowIfNull(region);
		ArgumentNullException.ThrowIfNull(service);
		_credentials = credentials;
		_region = region;
		_service = service;
	}

	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await SignRequestAsync(request, cancellationToken).ConfigureAwait(false);
		return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	protected override HttpResponseMessage Send(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		SignRequestAsync(request, cancellationToken).GetAwaiter().GetResult();
		return base.Send(request, cancellationToken);
	}

	private async Task SignRequestAsync(HttpRequestMessage request, CancellationToken ct)
	{
		var immutableCredentials = await _credentials.GetCredentialsAsync().ConfigureAwait(false);
		var now = DateTime.UtcNow;
		var dateStamp = now.ToString("yyyyMMdd");
		var amzDate = now.ToString("yyyyMMddTHHmmssZ");

		byte[] bodyBytes = [];
		if (request.Content is not null)
			bodyBytes = await request.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

		var bodyHash = HashSha256(bodyBytes);

		request.Headers.Remove("x-amz-date");
		request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
		request.Headers.Remove("x-amz-content-sha256");
		request.Headers.TryAddWithoutValidation("x-amz-content-sha256", bodyHash);

		if (!string.IsNullOrEmpty(immutableCredentials.Token))
		{
			request.Headers.Remove("x-amz-security-token");
			request.Headers.TryAddWithoutValidation("x-amz-security-token", immutableCredentials.Token);
		}

		if (request.Headers.Host is null && request.RequestUri is not null)
			request.Headers.Host = request.RequestUri.Host;

		var canonicalUri = request.RequestUri!.AbsolutePath;
		var canonicalQueryString = BuildCanonicalQueryString(request.RequestUri);

		var signedHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal)
		{
			["host"] = request.Headers.Host ?? request.RequestUri.Host,
			["x-amz-content-sha256"] = bodyHash,
			["x-amz-date"] = amzDate
		};

		if (!string.IsNullOrEmpty(immutableCredentials.Token))
			signedHeaders["x-amz-security-token"] = immutableCredentials.Token;

		var canonicalHeaders = string.Join("", signedHeaders.Select(h => $"{h.Key}:{h.Value.Trim()}\n"));
		var signedHeaderNames = string.Join(";", signedHeaders.Keys);

		var canonicalRequest = string.Join("\n",
			request.Method.Method,
			canonicalUri,
			canonicalQueryString,
			canonicalHeaders,
			signedHeaderNames,
			bodyHash);

		var credentialScope = $"{dateStamp}/{_region}/{_service}/aws4_request";
		var stringToSign = string.Join("\n",
			"AWS4-HMAC-SHA256",
			amzDate,
			credentialScope,
			HashSha256(Encoding.UTF8.GetBytes(canonicalRequest)));

		var signingKey = GetSigningKey(immutableCredentials.SecretKey, dateStamp, _region, _service);
		var signature = HmacSha256Hex(signingKey, stringToSign);

		var authorization =
			$"AWS4-HMAC-SHA256 Credential={immutableCredentials.AccessKey}/{credentialScope}, " +
			$"SignedHeaders={signedHeaderNames}, Signature={signature}";
		request.Headers.Remove("Authorization");
		request.Headers.TryAddWithoutValidation("Authorization", authorization);

		if (bodyBytes.Length > 0)
		{
			var contentType = request.Content?.Headers.ContentType;
			request.Content = new ByteArrayContent(bodyBytes);
			if (contentType is not null)
				request.Content.Headers.ContentType = contentType;
		}
	}

	private static string BuildCanonicalQueryString(Uri uri)
	{
		var query = uri.Query.TrimStart('?');
		if (string.IsNullOrEmpty(query))
			return string.Empty;

		var sortedParams = query.Split('&')
			.Select(p => p.Split('=', 2))
			.OrderBy(p => p[0], StringComparer.Ordinal)
			.Select(p => p.Length == 2 ? $"{p[0]}={p[1]}" : $"{p[0]}=");
		return string.Join("&", sortedParams);
	}

	private static string ToHexLower(byte[] bytes) =>
		Convert.ToHexString(bytes).ToLowerInvariant();

	private static string HashSha256(byte[] data)
	{
		var hash = SHA256.HashData(data);
		return ToHexLower(hash);
	}

	private static byte[] GetSigningKey(string secretKey, string dateStamp, string region, string service)
	{
		var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretKey), dateStamp);
		var kRegion = HmacSha256(kDate, region);
		var kService = HmacSha256(kRegion, service);
		return HmacSha256(kService, "aws4_request");
	}

	private static byte[] HmacSha256(byte[] key, string data) =>
		HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));

	private static string HmacSha256Hex(byte[] key, string data) =>
		ToHexLower(HmacSha256(key, data));
}
