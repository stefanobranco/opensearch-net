# OpenSearch .NET Client (Experimental)

[![CI](https://github.com/stefanobranco/opensearch-net/actions/workflows/ci.yml/badge.svg)](https://github.com/stefanobranco/opensearch-net/actions/workflows/ci.yml)
[![SB.OpenSearch.Client](https://img.shields.io/nuget/vpre/SB.OpenSearch.Client.svg?label=SB.OpenSearch.Client)](https://www.nuget.org/packages/SB.OpenSearch.Client)

> **This is an experimental, work-in-progress, AI-written ground-up rebuild of the OpenSearch .NET client.**
> It is **not** the official [opensearch-project/opensearch-net](https://github.com/opensearch-project/opensearch-net) client,
> it is **not** affiliated with or endorsed by that project, and it is **not** for production use.

## What is this?

A modern .NET client for [OpenSearch](https://opensearch.org/), built from scratch using AI (Claude) with heavy inspiration from:

- **[opensearch-java](https://github.com/opensearch-project/opensearch-java)** — architectural blueprint (transport, tagged unions, code generator)
- **[elasticsearch-net v8](https://github.com/elastic/elasticsearch-net)** — serialization patterns (ContextProvider, dual serializer, SourceConverter)
- **[OpenSearch API Specification](https://github.com/opensearch-project/opensearch-api-specification)** — generated types, endpoints, and enums

It is a spec-driven, System.Text.Json-based client. The goal is a clean, well-engineered client **on its own
terms** — an independent package published as `SB.OpenSearch.Client` / `SB.OpenSearch.Net`. There is no plan to
contribute it upstream; the honest "experimental / not the official package" framing is deliberate.

## Status

**Beta** — integration-tested in CI against OpenSearch **3.0.0, 3.4.0, and 3.7.0** on every commit
(see [`build-test.yml`](.github/workflows/build-test.yml)). Core functionality works, but the API surface is
incomplete and breaking changes are expected.

What works:
- Transport with retry logic, dead-node tracking, handler rotation for DNS refresh
- System.Text.Json serialization with snake_case naming, tagged unions, enum converters
- Code-generated types across **19 namespaces** (full opensearch-java parity), covering **388 of 481** spec
  operations — search, index, get, delete, bulk, multi-search/get, cat, cluster, indices, ingest, snapshot,
  tasks, ISM, k-NN, ML, security, and more (see [`API_COVERAGE.md`](API_COVERAGE.md))
- Fluent descriptors with expression-based field resolution
- Query DSL (57 query types) and aggregations (65 types) with typed builders
- Scalar query values accept primitives directly (`new TermQuery { Value = "active" }`) via `FieldValue`
- NDJSON streaming for Bulk and Multi-Search
- AWS SigV4 authentication

What's incomplete or missing:
- A handful of polymorphic response shapes are still read as raw `JsonElement` (e.g. `cluster.state`,
  streaming ML predict/execute)
- `nodes.info` / `nodes.stats` expose the `_nodes` summary but not per-node details
- No Newtonsoft.Json bridge yet
- Integration tests cover 11 namespaces against a real cluster; the rest are covered by serialization fixtures only
- `Node` is defined in both `OpenSearch.Net` (transport) and `OpenSearch.Client` (cluster info); qualify it if you import both namespaces

## Quick Start

```csharp
using OpenSearch.Client;   // one namespace exposes the client, query DSL, requests and responses

var client = new OpenSearchClient(new Uri("https://localhost:9200"));

// Fluent form — expression-based fields, primitives pass straight through (no JsonElement wrapping):
var response = client.Search<MyDoc>(s => s
    .Index("my-index")
    .Query(q => q.Match(f => f.Title!, m => m.Query("opensearch")))
    .Size(10));

// …or the object-initializer form:
var response2 = client.Search<MyDoc>(new SearchRequest
{
    Index = ["my-index"],
    Query = QueryContainer.Match("title", new MatchQuery { Query = "opensearch" }),
    Size = 10,
});

foreach (var doc in response.Documents())
    Console.WriteLine(doc.Title);
```

## Architecture

| Layer | Package | Description |
|-------|---------|-------------|
| Transport | `OpenSearch.Net` | HTTP transport, node pool, retry logic, diagnostics |
| Client | `OpenSearch.Client` | Typed client, serialization, generated types, descriptors |
| Code Generator | `OpenSearch.CodeGen` | Generates C# from the OpenSearch API specification |

Targets **net8.0** and **net10.0**. No netstandard2.0.

## Building

```bash
dotnet build
dotnet test
```

The generated client under `src/OpenSearch.Client/Generated` is reproducible from the vendored spec via
[`build/regenerate.sh`](build/regenerate.sh); CI fails if it drifts.

## License

[Apache v2.0](LICENSE.txt)

## Acknowledgements

This project is a fork of [opensearch-project/opensearch-net](https://github.com/opensearch-project/opensearch-net).
The rebuild was written using [Claude](https://claude.ai) (Anthropic) as an AI coding assistant.
