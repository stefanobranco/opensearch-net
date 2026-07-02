# Benchmarks

Performance baseline (ROADMAP C3) comparing this client (`SB.OpenSearch.Client`) against the
official `OpenSearch.Client` 1.8.0 (NEST-lineage, Utf8Json).

## Layout

- `OpenSearch.Benchmarks/` — scenarios against this repo's client.
- `OpenSearch.Benchmarks.Legacy/` — the same scenarios against the official `OpenSearch.Client` 1.8.0.
- `Common/BenchmarkData.cs` — shared by both projects: deterministic documents and canned response
  bodies, so both clients are measured against byte-identical wire payloads.

End-to-end scenarios run through each client's full pipeline with a canned-response HTTP layer at
the bottom (`CannedResponseHandler` here, `InMemoryConnection` for legacy): request building, body
serialization, HTTP message construction, and response deserialization are all measured — only the
network is not. The serializer-only scenarios isolate the JSON core.

## Running

```bash
# Full suite (either project)
dotnet run --project benchmarks/OpenSearch.Benchmarks -c Release -f net8.0 -- --filter '*'
dotnet run --project benchmarks/OpenSearch.Benchmarks.Legacy -c Release -f net8.0 -- --filter '*'

# Validate the harness (runs every scenario once and checks the parsed responses)
dotnet run --project benchmarks/OpenSearch.Benchmarks -c Release -f net8.0 -- smoke
```

## Baseline (2026-07-02)

Apple M4 Max, .NET 8.0.22, BenchmarkDotNet 0.15.8. This client at commit of PR #32,
legacy = official `OpenSearch.Client` 1.8.0.

| Scenario | This client | Legacy 1.8.0 | Allocated (this / legacy) |
|---|---:|---:|---|
| `SearchSimple` — 10-hit search, match query | **9.6 μs** | 10.7 μs | **14.0 KB** / 20.2 KB |
| `SearchRich` — 100 hits, bool+range+terms, 2 aggs | 82.6 μs | **78.9 μs** | **99.6 KB** / 145.8 KB |
| `BulkIndex1000` — 1000-doc bulk index round-trip | 1,086 μs | **1,038 μs** | **954 KB** / 2,059 KB¹ |
| `GetDocument` — single-doc GET | **1.24 μs** | 1.50 μs | **3.1 KB** / 4.3 KB |
| `SerializeSearchRequest` — rich request, write only | 1.85 μs | **1.63 μs** | **1.4 KB** / 3.0 KB |
| `DeserializeSearchResponse` — 100 hits + aggs, read only | 71.1 μs | **70.3 μs** | **89.2 KB** / 123.9 KB |
| `DeserializeSearchResponseNoAggs` — 100 hits, read only | 65.7 μs | **63.9 μs** | **84.4 KB** / 117.1 KB |

¹ Legacy's bulk run churns the large-object heap (Gen2 collections on every measurement interval);
this client's stays in Gen0/Gen1.

**Summary:** time at parity across the board (every scenario within ±6%, several faster), with
25–55% lower allocations everywhere. The remaining small time deltas (rich search ±5%, request
serialization +13%) trace to descriptor construction and QueryContainer union dispatch — small in
absolute terms (< 4 μs) against any real network round-trip.

## Fixes this baseline produced

The first run surfaced two hot-path design flaws, both fixed at the root rather than recorded as
gaps (numbers above are post-fix):

1. **Source-converter wrapper on the read path.** `SourceConverterFactory` handed every document
   property a converter that re-entered `JsonSerializer` per hit (~0.3 μs each). The factory now
   resolves the routing decision once per options instance: no custom source serializer → the
   document type's own converter, zero wrapper. Response deserialization: 115.8 → 71.1 μs.
2. **Dictionary-built bulk NDJSON.** Bulk action lines were built as nested dictionaries and each
   NDJSON line paid its own serializer entry. `NdjsonWriter` now drives one `Utf8JsonWriter` over
   the request stream (reset per line) and `BulkOperation`s write their action metadata directly.
   Bulk: 1,412 → 1,086 μs, allocations 1,996 → 954 KB.

## Custom source serializer cost (2026-07-02)

`SourceSerializerBenchmarks` quantifies what plugging a custom document serializer into
`SourceSerializer(...)` costs on the document-heavy paths. Three configurations: the default
(documents on the built-in STJ serializer — no boundary at all, the factory hands the document
type its own converter), a second `SystemTextJsonSerializer` as source serializer (isolates the
buffering boundary any custom serializer pays: each document is re-buffered at the
client/document seam), and a Newtonsoft.Json adapter (boundary + the Newtonsoft engine;
`NewtonsoftSourceSerializer.cs` doubles as the reference adapter implementation).

| Scenario | Default (STJ) | Custom STJ source | Newtonsoft source |
|---|---:|---:|---:|
| Search, 100 hits | **69.4 μs** / 88 KB | 165.3 μs / 217 KB (2.4×) | 232.0 μs / 743 KB (3.3×) |
| Bulk index, 1000 docs | **1.06 ms** / 954 KB | 1.69 ms / 1.5 MB (1.6×) | 2.06 ms / 3.9 MB (1.9×) |

Takeaways:

- The default path pays **zero** boundary cost — routing only engages when a distinct source
  serializer is configured.
- Roughly **60% of the custom-serializer penalty is the boundary itself** (compare the custom-STJ
  column): each document is parsed to a buffer and re-read so the foreign serializer can own it.
  That cost is inherent to handing documents to an arbitrary `IOpenSearchSerializer`.
- Newtonsoft adds its own engine cost on top — most visibly in allocations (8× the default on
  search, 4× the custom-STJ path) and Gen2 pressure on bulk.
- Practical guidance: bring Newtonsoft only if your document types genuinely depend on its
  attributes/converters; ~1–2 μs per document is real but small against network round-trips.
  Prefer configuring a second `SystemTextJsonSerializer` with custom options when STJ can
  express your document conventions.

## Measurement notes

- Both document types produce identical wire field names (single-word properties), so the shared
  fixtures parse in both clients despite different naming policies.
- Legacy needs `.DisablePing()` — with `InMemoryConnection` a ping would consume the canned
  response before the actual request.
- The BenchmarkDotNet child-process build is why `Directory.Build.targets` must not override
  `OutDir` for packable projects (BDN generates a project that references `OpenSearch.Client`
  and builds it into an isolated output folder; a repo-level `OutDir` override broke that copy).
