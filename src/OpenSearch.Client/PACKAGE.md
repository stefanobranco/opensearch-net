# SB.OpenSearch.Client

> **Experimental, AI-written rebuild. Not the official [OpenSearch.Client](https://www.nuget.org/packages/OpenSearch.Client) package. Not for production use.**

Strongly-typed .NET client for [OpenSearch](https://opensearch.org/), generated from the [OpenSearch API specification](https://github.com/opensearch-project/opensearch-api-specification).

## What is this?

A ground-up rebuild of the OpenSearch .NET client, written using AI (Claude) with heavy inspiration from:

- **opensearch-java** — architectural blueprint
- **elasticsearch-net v8** — serialization patterns
- **OpenSearch API Specification** — code-generated types and endpoints

Built with System.Text.Json, targeting net8.0 and net10.0.

## Quick Start

```csharp
using OpenSearch.Client;
using OpenSearch.Client.Core;

var client = new OpenSearchClient(new Uri("https://localhost:9200"));

var response = client.Search<MyDoc>(s => s
    .Index(["my-index"])
    .Query(q => q.Match(f => f.Title!, m => m.Query("opensearch")))
    .Size(10));

foreach (var doc in response.Documents())
    Console.WriteLine(doc.Title);
```

## Features

- 480+ code-generated API endpoints
- Fluent query DSL with 55 query types and expression-based field resolution
- Aggregations with typed bucket and metric accessors
- Bulk and Multi-Search with NDJSON streaming
- Suggest (term, phrase, completion)
- Non-throwing by default with `IsValid` / `ServerError` pattern

## Links

- [GitHub Repository](https://github.com/stefanobranco/opensearch-net)
- [Official OpenSearch .NET Client](https://github.com/opensearch-project/opensearch-net)
