# OpenSearch.Client

Strongly-typed .NET client for [OpenSearch](https://opensearch.org/), generated from the [OpenSearch API specification](https://github.com/opensearch-project/opensearch-api-specification).

## Features

- **385 API operations** across 19 namespaces (core, indices, cluster, cat, security, ml, and more)
- **Fluent descriptor API** for building requests with lambda syntax
- **Tagged union types** for polymorphic APIs (Query DSL, Aggregations, Mappings)
- **Field-keyed query convenience** — `QueryContainer.Term("status", query)` instead of verbose dictionary construction
- **System.Text.Json serialization** — no Utf8Json dependency, full async support
- **Sync and async** methods with `CancellationToken` support on every operation
- **Targets net8.0 and net10.0**

## Quick Start

```csharp
using OpenSearch.Client;
using OpenSearch.Client.Core;
using OpenSearch.Client.Common;

var client = new OpenSearchClient(new Uri("https://localhost:9200"));

// Index a document
client.Core.Index(new IndexRequest
{
    Index = "my-index",
    Id = "1",
    Body = new { title = "Hello", tags = new[] { "opensearch" } }
});

// Search with typed response
var response = client.Core.Search<MyDocument>(new SearchRequest
{
    Index = ["my-index"],
    Size = 10,
    Query = QueryContainer.Match("title", new MatchQuery
    {
        Query = JsonSerializer.SerializeToElement("hello")
    })
});

foreach (var hit in response.Hits!.Hits!)
    Console.WriteLine(hit.Source!.Title);
```

## Fluent Descriptor API

```csharp
var response = client.Core.Search<MyDocument>(d => d
    .Index(["my-index"])
    .Size(10)
    .Query(q => q.Bool(b => b
        .Must(
            m => m.Match("title", t => t.Query(JsonSerializer.SerializeToElement("hello"))),
            m => m.Exists(e => e.Field("tags"))
        )
    ))
);
```

## Links

- [GitHub Repository](https://github.com/opensearch-project/opensearch-net)
- [OpenSearch Documentation](https://opensearch.org/docs/latest/)
- [API Reference](https://opensearch.org/docs/latest/api-reference/)
