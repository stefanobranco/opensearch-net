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

## Measurement notes

- Both document types produce identical wire field names (single-word properties), so the shared
  fixtures parse in both clients despite different naming policies.
- Legacy needs `.DisablePing()` — with `InMemoryConnection` a ping would consume the canned
  response before the actual request.
- The BenchmarkDotNet child-process build is why `Directory.Build.targets` must not override
  `OutDir` for packable projects (BDN generates a project that references `OpenSearch.Client`
  and builds it into an isolated output folder; a repo-level `OutDir` override broke that copy).
