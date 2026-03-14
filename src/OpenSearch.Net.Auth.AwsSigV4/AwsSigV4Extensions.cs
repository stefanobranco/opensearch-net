using Amazon.Runtime;

namespace OpenSearch.Net.Auth.AwsSigV4;

/// <summary>
/// Extension methods for configuring AWS SigV4 authentication on the OpenSearch transport.
/// </summary>
public static class AwsSigV4Extensions
{
	/// <summary>
	/// Configures the transport to sign all HTTP requests using AWS Signature Version 4.
	/// </summary>
	/// <param name="builder">The transport configuration builder.</param>
	/// <param name="credentials">
	/// AWS credentials used to sign each request (e.g.,
	/// <see cref="BasicAWSCredentials"/>, <see cref="SessionAWSCredentials"/>,
	/// or credentials from <see cref="FallbackCredentialsFactory"/>).
	/// </param>
	/// <param name="region">The AWS region (e.g., <c>us-east-1</c>).</param>
	/// <param name="service">
	/// The AWS service name used in the credential scope.
	/// Defaults to <c>es</c> for Amazon OpenSearch Service.
	/// Use <c>aoss</c> for Amazon OpenSearch Serverless.
	/// </param>
	/// <returns>The builder for fluent chaining.</returns>
	public static TransportConfiguration.Builder UseAwsSigV4(
		this TransportConfiguration.Builder builder,
		AWSCredentials credentials,
		string region,
		string service = "es") =>
		builder.HttpMessageHandlerFactory(inner =>
			new AwsSigV4HttpMessageHandler(credentials, region, service, inner));
}
