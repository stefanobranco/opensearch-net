# OpenSearch.Client

Strongly-typed .NET client for [OpenSearch](https://opensearch.org/), generated from the [OpenSearch API specification](https://github.com/opensearch-project/opensearch-api-specification).

## Quick Start

```csharp
using OpenSearch.Client;
using OpenSearch.Client.Core;
using OpenSearch.Client.Common;

var client = new OpenSearchClient(new Uri("https://localhost:9200"));
```

## Search with Fluent Descriptors

Use `Search<T>(Action<SearchRequestDescriptor<T>>)` for expression-based field selection:

```csharp
var response = client.Core.Search<MyDoc>(s => s
    .Index(["my-index"])
    .Query(q => q
        .Bool(b => b
            .Must(
                m => m.Match(f => f.Title!, t => t.Query("opensearch")),
                m => m.Term(f => f.Status!, t => t.Value("active"))
            )
            .Filter(f => f.Exists(e => e.Field("tags")))
        ))
    .Sort(SortOptions.Descending("_score"))
    .Size(10)
    .From(0)
    .Source(SourceConfig.Enabled(true)));
```

## Search with Request Objects

```csharp
var response = client.Core.Search<MyDoc>(new SearchRequest
{
    Index = ["my-index"],
    Size = 10,
    Query = QueryContainer.Match("title", new MatchQuery { Query = "opensearch" }),
});
```

## Reading Search Results

```csharp
// Convenience accessors (extension methods)
if (response.IsValid())
{
    long total = response.Total();          // total hit count
    var docs = response.Documents();         // IReadOnlyList<MyDoc>

    foreach (var doc in docs)
        Console.WriteLine(doc.Title);
}

// Hit-level metadata
foreach (var hit in response.Hits!.Hits!)
{
    Console.WriteLine($"{hit.Id}: {hit.Source!.Title} (score: {hit.Score})");
}

// Total hits with relation
var totalHits = response.Hits!.Total!;       // TotalHits type
Console.WriteLine($"{totalHits.Value} ({totalHits.Relation})");
```

## Aggregations

```csharp
var response = client.Core.Search<MyDoc>(s => s
    .Index(["products"])
    .Size(0)
    .Aggregations(a => a
        .Terms("by_status", t => t.Field("status"))
        .Avg("avg_price", t => t.Field("price"))));

// Typed aggregation access
var aggs = response.Aggs();
var avgPrice = aggs.Average("avg_price");

foreach (var bucket in aggs.Terms("by_status")!)
{
    Console.WriteLine($"{bucket.Key}: {bucket.DocCount}");
    // Sub-aggregation chaining
    var subAvg = bucket.Aggregations?.Average("nested_avg");
}
```

## Terms Query with Expressions

```csharp
// Expression-based (type-safe field names)
.Query(q => q.Terms(f => f.Status!, "active", "pending"))
.Query(q => q.Terms<int>(f => f.Priority!, 1, 2, 3))

// String-based
.Query(q => q.Terms("status", "active", "pending"))

// Descriptor with boost
.Query(q => q.Terms(t => t
    .Field<MyDoc>(f => f.Status!, "active", "pending")
    .Boost(1.5f)))
```

## Suggest

```csharp
var response = client.Core.Search<MyDoc>(s => s
    .Index(["my-index"])
    .Suggest(sg => sg
        .Text("opensearh")
        .Completion("autocomplete", c => c.Field("title.suggest").Size(5), prefix: "open")
        .Term("did_you_mean", t => t.Field("content").Size(3))));

// Typed suggest access
var suggestions = response.Suggestions();
var completions = suggestions.GetCompletion("autocomplete");
var termSuggestions = suggestions.GetTerm("did_you_mean");
```

## Multi-Search

```csharp
var msearch = new MsearchRequest { Index = "default" };

msearch
    .AddSearch<MyDoc>("books", s => s
        .Query(q => q.Match(f => f.Title!, t => t.Query("opensearch")))
        .Size(10))
    .AddSearch<MyDoc>("logs", s => s
        .Size(0)
        .Aggregations(a => a.Avg("avg_duration", t => t.Field("duration"))));

var msResponse = client.Core.Msearch(msearch);

// Typed access per sub-search
var hits = msResponse.Responses![0].GetHits<MyDoc>();
var aggs = msResponse.Responses[1].GetAggregations();
```

## Multi-Get

```csharp
var response = client.Core.Mget(new MgetRequest
{
    Index = "my-index",
    Ids = ["1", "2", "3"]
});

foreach (var doc in response.GetDocs<MyDoc>())
{
    if (doc.Found)
        Console.WriteLine($"{doc.Id}: {doc.Source!.Title}");
}
```

## Index, Delete, Bulk

```csharp
// Index
client.Core.Index(new IndexRequest
{
    Index = "my-index",
    Id = "1",
    Body = new { title = "Hello", tags = new[] { "opensearch" } }
});

// Delete
client.Core.Delete(new DeleteRequest { Index = "my-index", Id = "1" });

// Bulk
client.Core.Bulk(new BulkRequest
{
    Index = "my-index",
    Operations =
    [
        new BulkIndexOperation<MyDoc>(new MyDoc { Title = "First" }) { Id = "1" },
        new BulkIndexOperation<MyDoc>(new MyDoc { Title = "Second" }) { Id = "2" },
        new BulkDeleteOperation { Id = "3" },
    ]
});
```

## API Structure

All operations are accessed via namespace properties on `OpenSearchClient`:

| Namespace | Property | Examples |
|-----------|----------|----------|
| Core | `client.Core` | Search, Index, Get, Delete, Bulk, Mget, Msearch, Scroll |
| Indices | `client.Indices` | Create, Delete, PutMapping, PutSettings, Exists |
| Cluster | `client.Cluster` | Health, GetSettings, PutSettings |
| Cat | `client.Cat` | Indices, Aliases, Health, Nodes |
| Security | `client.Security` | GetUser, PutRole, CreateUser |
| Ingest | `client.Ingest` | PutPipeline, GetPipeline, DeletePipeline |

Each method has sync, async, request-object, and descriptor overloads.

## Links

- [GitHub Repository](https://github.com/opensearch-project/opensearch-net)
- [OpenSearch Documentation](https://opensearch.org/docs/latest/)
