# OpenSearch .NET Client (Experimental)

> **This is an experimental, work-in-progress, AI-written ground-up rebuild of the OpenSearch .NET client.**
> It is **not** the official [opensearch-project/opensearch-net](https://github.com/opensearch-project/opensearch-net) client.
> Do not use in production.

## What is this?

A modern .NET client for [OpenSearch](https://opensearch.org/), built from scratch using AI (Claude) with heavy inspiration from:

- **[opensearch-java](https://github.com/opensearch-project/opensearch-java)** — architectural blueprint (transport, tagged unions, code generator)
- **[elasticsearch-net v8](https://github.com/elastic/elasticsearch-net)** — serialization patterns (ContextProvider, dual serializer, SourceConverter)
- **[OpenSearch API Specification](https://github.com/opensearch-project/opensearch-api-specification)** — generated types, endpoints, and enums (spec version 0.3.0 / API version 2.16.0)

The goal is a spec-driven, System.Text.Json-based client that can eventually be contributed upstream to replace the aging NEST/Utf8Json-based v1.x client.

## Status

**Alpha** — tested against OpenSearch 3.4. Core functionality works but the API surface is incomplete and breaking changes are expected.

What works:
- Transport with retry logic, dead-node tracking, handler rotation for DNS refresh
- System.Text.Json serialization with snake_case naming, tagged unions, enum converters
- Code-generated types from the OpenSearch API specification (480+ endpoints)
- Search, Index, Get, Delete, Bulk, Multi-Search, Multi-Get, Scroll
- Fluent descriptors with expression-based field resolution
- Aggregations (terms, date_histogram, histogram, range, filter, nested, stats, etc.)
- Query DSL (55 query types with typed builders)
- Suggest (term, phrase, completion)
- NDJSON streaming for Bulk and Multi-Search
- AWS SigV4 authentication

What's incomplete or missing:
- Some advanced aggregation response accessors (percentiles, geo_bounds, top_hits)
- Custom SourceSerializer not yet wired to generated Hit<T>.Source
- Some root-level convenience shortcuts (IndexMany, DeleteByQuery, etc.)
- No Newtonsoft.Json bridge yet
- Limited integration test coverage for generated endpoints

## Quick Start

```csharp
using OpenSearch.Client;
using OpenSearch.Client.Core;

var client = new OpenSearchClient(new Uri("https://localhost:9200"));

// Search with fluent descriptors
var response = client.Search<MyDoc>(s => s
    .Index(["my-index"])
    .Query(q => q.Match(f => f.Title!, m => m.Query("opensearch")))
    .Size(10));

foreach (var doc in response.Documents())
    Console.WriteLine(doc.Title);
```

## Architecture

| Layer | Package | Description |
|-------|---------|-------------|
| Transport | `OpenSearch.Net` | HTTP transport, node pool, retry logic, diagnostics |
| Client | `OpenSearch.Client` | Typed client, serialization, generated types, descriptors |
| Code Generator | `OpenSearch.CodeGen` | Generates C# from OpenSearch API specification |

Targets **net8.0** and **net10.0**. No netstandard2.0.

## Building

```bash
dotnet build
dotnet test
```

## License

[Apache v2.0](LICENSE.txt)

## Acknowledgements

This project is a fork of [opensearch-project/opensearch-net](https://github.com/opensearch-project/opensearch-net).
The rebuild was written using [Claude](https://claude.ai) (Anthropic) as an AI coding assistant.
