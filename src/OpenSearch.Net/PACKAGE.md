# OpenSearch.Net

Low-level transport library for the [OpenSearch](https://opensearch.org/) .NET client.

## Overview

OpenSearch.Net provides the HTTP transport, node management, and serialization infrastructure that `OpenSearch.Client` builds on. You typically don't use this package directly — install `OpenSearch.Client` instead for the full typed client experience.

Use OpenSearch.Net directly when you need:
- Custom transport behavior or node pool strategies
- Raw HTTP access to OpenSearch without generated types
- To build your own higher-level client

## Features

- **HttpClient-based transport** with automatic handler rotation for DNS refresh
- **Multi-node support** with round-robin selection, dead-node tracking, and exponential backoff
- **System.Text.Json serialization** with async streaming support
- **AWS SigV4 authentication** support
- **Targets net8.0 and net10.0**

## Quick Start

```csharp
using OpenSearch.Net;

var config = TransportConfiguration
    .Create(new Uri("https://localhost:9200"))
    .Build();

var transport = new HttpClientTransport(config);
```

## Links

- [GitHub Repository](https://github.com/opensearch-project/opensearch-net)
- [OpenSearch Documentation](https://opensearch.org/docs/latest/)
- [API Reference](https://opensearch.org/docs/latest/api-reference/)
