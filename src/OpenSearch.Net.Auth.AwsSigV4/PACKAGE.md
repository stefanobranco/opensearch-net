# SB.OpenSearch.Net.Auth.AwsSigV4

> **Experimental, AI-written rebuild. Not an official [opensearch-net](https://github.com/opensearch-project/opensearch-net) package. Not for production use.**

AWS SigV4 request signing for the `SB.OpenSearch.Net` transport — authenticate against Amazon OpenSearch Service (or OpenSearch Serverless) using standard AWS credentials.

## Usage

```csharp
using Amazon.Runtime;
using OpenSearch.Net;
using OpenSearch.Net.Auth.AwsSigV4;

var transport = TransportConfiguration
    .Create(new Uri("https://my-domain.eu-central-1.es.amazonaws.com"))
    .UseAwsSigV4(FallbackCredentialsFactory.GetCredentials(), "eu-central-1")   // service defaults to "es"; use "aoss" for Serverless
    .Build();
```

## Links

- [GitHub Repository](https://github.com/stefanobranco/opensearch-net)
