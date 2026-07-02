# Roadmap — "clean as possible"

Internal cleanup plan for the v2 rebuild. Goal: make this client genuinely clean and
well-engineered **on its own terms**, with no push for upstream adoption.

## Direction (decided 2026-06-30)

- **Independent package**, published to nuget.org as `SB.OpenSearch.Client` / `SB.OpenSearch.Net`.
- **No upstream push** — no PRs to opensearch-project, no maintainer/adoption discussions.
  If it organically becomes the client one day, fine; it won't be pushed there.
- **Keep the honest framing** — "experimental / not the official package" disclaimers stay.
  They're a deliberate, non-presumptuous posture (opensearch-net is an official AWS repo).
- "Release properly" = a **technical + process** bar, not an adoption bar.

## Current state (as of beta.4)

- Phases 1–3 of the original build plan essentially done: transport, serialization, code
  generator, ~349 request/response pairs, 65 query types, 77 aggregation types, NDJSON, SigV4.
- Clean code (zero TODO/FIXME), `Nullable` + warnings-as-errors, 441 test methods, 23
  integration-test files against a real cluster.
- Gaps: no CI/CD, releases hand-run from a laptop; `snapshot` + `tasks` namespaces missing;
  uneven integration depth for generated-but-untested namespaces; no perf testing.

---

## Phase A — Release infrastructure (foundation, do first)

- [x] **A1. CI on PR** — reusable `build-test.yml` (unit job runs the whole solution; integration
      tests self-skip via `[SkipIfNoCluster]`, so no hardcoded project list) + an integration job
      against an OpenSearch service container (mirrors `docker-compose.yaml`). `ci.yml` calls it on
      push/PR. A composite action (`.github/actions/setup`) centralizes SDK setup + NuGet caching.
      The integration job matrixes over OpenSearch 3.0.0 / 3.4.0 / 3.7.0 (`fail-fast: false`);
      2.x is an opt-in addition once its tests are validated.
- [x] **A2. Automated publish** — `release.yml`: on a semver tag, gates on the same `build-test.yml`,
      then packs (version derived from the tag) + `nuget push` + GitHub Release.
      _Requires `NUGET_API_KEY` repo secret._
- [x] **A3. Release hygiene** in `Directory.Build.props` — SourceLink (built into the .NET 8+ SDK,
      no package reference), `PublishRepositoryUrl`, `EmbedUntrackedSources`, `IncludeSymbols` +
      `snupkg`, `Deterministic`, and `ContinuousIntegrationBuild` (CI only). Verified: pack produces
      a `.snupkg` and the nuspec embeds the repo URL + commit.
- [x] **A4. Backfill tags — won't do.** A faithful backfill isn't achievable: `alpha.5`/`alpha.8`
      have no identifiable bump commit, a `bump to alpha.12` commit was never published, and version
      strings collide for mechanical matching. SourceLink (A3) now stamps every future package with
      its commit, so historical tags would be guesswork for no benefit. Tagged from `beta.4` forward.
- [x] **Hardening** — `.github/dependabot.yml` (github-actions + nuget, grouped weekly) and
      least-privilege `permissions: contents: read` on `ci.yml`.

## Phase B — Functional completeness (core gaps)

- [~] **B1. `snapshot` namespace** — generated + wired (11 ops, `SnapshotInfo`/rich Create/Restore
      bodies). Serialization fixtures + integration tests still pending.
- [~] **B2. `tasks` namespace** — generated + wired (Cancel/Get/List, `TaskInfo`). Serialization
      fixtures + integration tests still pending.
- [x] **B3. Reviewed remaining namespaces.** Wired every namespace opensearch-java ships that we
      were missing — `snapshot`, `tasks`, `search_pipeline`, `search_relevance`, `ubi` — reaching
      **19/19 java parity**. The other 17 spec namespaces are shipped by neither client (see
      `API_COVERAGE.md`) and are left unwired pending demand.

  Generator correctness fixes surfaced while wiring (all resolved in the follow-up generator PR):
  - [x] Request bodies shaped as `type: array`, `oneOf`/`anyOf`, `$ref`-alias chains, or scalars were
    silently dropped (`GetBody => null`). The generator now follows alias chains (so `ism.put_policy`
    flattens to a typed `Policy` field), merges discriminator-less `oneOf`/`anyOf`-of-objects bodies
    into a typed superset (the `search_relevance.put_experiments`/`put_judgments` bodies), and models
    array/scalar bodies as a typed `Body` payload (the `security.patch_*` family carries
    `List<PatchOperation>`). A validator census reports every typed raw body.
  - [x] `search_pipeline` processor lists (`RequestProcessor`/`ResponseProcessor`/`PhaseResultsProcessor`)
    are now typed tagged unions (externally-tagged, like `QueryContainer`) — the generator learned the
    "oneOf of single-property wrapper objects" pattern.
  - [x] `text/plain` responses (`ubi.initialize`, `nodes.hot_threads`, `cat.help`) now capture the raw
    body in a `Value` string instead of JSON-deserializing it (a runtime failure).

## Phase C — Test depth / verification

- [~] **C1.** Integration smoke tests for generated-but-untested namespaces. Added CI-verified
      integration tests (3.0/3.4/3.7) for **nodes, tasks, search_pipeline, snapshot (repo lifecycle),
      ism, knn (vector search + stats)** — 11 namespaces now have integration coverage (with the
      pre-existing core/cat/cluster/indices/ingest). The CI container now sets `path.repo`.
      This surfaced a real bug (epoch integer fields overflowing Int32 — now promoted to long).
      **Not CI-testable here** (covered by serialization fixtures instead): `security` (plugin disabled
      in CI), `ltr`/`ubi`/`search_relevance` (not in the base image), `ingestion`/`dangling_indices`
      (need external sources/scenarios). `ml`/`geospatial` are bundled but need heavier setup (model
      registration / external datasources) — possible stretch.
- [~] **C2.** Fill roundtrip/response-deserialization coverage gaps. Response fixtures for
      snapshot/tasks/cluster/ingest, plus the recovered responses below (the request/DSL side was
      already well covered). Remaining: response parsing for more namespaces.
- [~] **C4. Thin/empty response types (feature gap surfaced while writing response fixtures).** Several
      responses generated empty types that discarded cluster data opensearch-java exposes. Fixed at the
      generator root by mirroring the request-body handling: response `$ref`-alias chains are now
      followed (recovers `ism.get_policy` and ~12 others), and discriminator-less `oneOf`/`anyOf`
      responses merge into a typed superset (recovers `delete_by_query`/`update_by_query`/`reindex` —
      the full bulk-by-scroll result *or* the async `{task}` form — plus `indices.open`). ~18 responses
      recovered total. Array responses are now modelled as `List<Item>` (a new response shape alongside
      dictionary responses), so the **23 `cat.*` APIs** deserialize their record arrays instead of being
      discarded; dotted record columns (`docs.count`, `store.size`) are kept as `[JsonPropertyName]`
      fields (recovers the full cat row — `IndicesRecord` went 6 → 145 fields — and adds the flat
      dotted-settings form to ~17 shared types).
      **Still empty/loose:** a handful that are genuinely bodiless (voting-config 202s), streaming
      (`ml` predict/execute streams), or freeform (`cluster.state`), plus `nodes.info`/`nodes.stats`
      which expose only the `_nodes` summary (no per-node details).
- [x] **C5. Punted-union ergonomics (surfaced while writing the README Quick Start).** The single most
      common query — a `match`/`term` on a string — required `JsonSerializer.SerializeToElement("...")`
      because the spec's scalar/union value schemas were punted to opaque `JsonElement` in the generator's
      override table. That table read as "handled" to our own tooling, so no metric flagged it; the gap
      only surfaced when the first genuine consumer-perspective exercise (a compiling Quick Start) forced
      it. Fixed at the root: the spec's named `FieldValue` (anyOf of scalars) and 10 further punted unions
      (`Like`, `TermsQueryField`, `TermsInclude`, `Context`, `TrackHits`, `IndexSettingsMergePolicy`,
      `GeoBounds`, `XyLocation`, `TaskInfos`, `AggregateOrder`) are now hand-written types with implicit
      conversions + converters, registered through the same override table (mirroring `GeoLocation` /
      `BucketsPath`). So `new TermQuery { Value = "active" }` and `TrackTotalHits = true` just work, and the
      hand-written descriptor extensions that wrapped `SerializeToElement` are deleted. To stop this class
      of gap recurring, the codegen validator now **censuses every override→`JsonElement`** as a tracked
      degradation (not "handled"). **Deliberately left opaque, documented:** `DecayPlacement` (absorbed by
      `DecayFunction`), `CompletionContext` (nests the still-unmodeled `GeoHashPrecision` int|string union),
      `SegmentReplicationStats` (version-divergent response encodings).
- [x] **C6. Release-readiness audit for beta.5 (production-adoption bar).** Auditing "ready for our
      production" surfaced two real gaps, both fixed at the root:
      - **`SourceSerializer` was accepted but silently inert** — `SourceConverter<T>` existed with zero
        references; a configured custom document serializer did nothing. Now wired on both sides: the
        generator emits `[JsonConverter(typeof(SourceConverterFactory))]` on `TDocument` fields
        (`Hit<T>.Source`, `GetResult<T>`/`GetResponse<T>.Source`, `InlineGet<T>` — found by the uniform
        rule, not by enumeration), and document write paths (`index`/`create` bodies via an explicit
        generator list, bulk NDJSON ops via `SourceDocument`) route through the source serializer.
        Two converter modes on purpose: type-level `SourceConverter<T>` keeps its strict delegate-or-throw
        contract; the property-attribute `SourcePropertyConverter<T>` falls back to in-place
        (de)serialization so generated types stay usable as plain POCOs and the default path pays no
        buffering (a first cut that reused one converter for both modes stack-overflowed under the
        type-level contract — caught by the full suite, split into two).
      - **`SB.OpenSearch.Net.Auth.AwsSigV4` was packable but never packed** — missing from `release.yml`
        and missing its `SB.` PackageId/disclaimer/readme. All added.
      Verified beyond CI: packed all three as beta.5 and ran a **consumer smoke test** — a fresh console
      app restoring from the local feed, compiling the README Quick Start verbatim, asserting wire format
      and the custom-SourceSerializer path. `SourceSerializerTests` pins the dual-serializer contract.
- [~] **C7. RC-readiness track (decided 2026-07-02).** The bar for calling a version RC, worked in order:
  1. [x] **Aggregation result accessors to builder parity.** You could fluently build percentiles /
     filters / geo_bounds / date_histogram / range / *_bucket pipeline aggregations but not read their
     results without raw JsonElement digging. Added typed accessors (`Percentiles`/`PercentileRanks`/
     `PercentilesBucket` with keyed+array wire forms, `Filters` named+anonymous, `GeoBounds`,
     `MaxBucket`/`MinBucket` with winning keys, `AvgBucket`/`SumBucket`/`StatsBucket`/
     `ExtendedStatsBucket`). Along the way: the `filters` aggregation *request* was unusable —
     `BucketsQueryContainer` generated as an empty tagged union (one of the 19 census empties); now a
     hand-written keyed-or-array union of `QueryContainer`, so named filters are constructible
     (`Filters = new Dictionary<string, QueryContainer> {...}`). Fixtures for every accessor + 5
     CI-verified integration tests (incl. geo_point mapping and a max_bucket pipeline).
  2. [x] **Async integration mirror + scroll/PIT tests.** The async overloads (distinct transport,
     NDJSON-streaming, and deserialization code paths) had zero integration coverage, and no test
     exercised stateful pagination. Added CI-verified flows: full document lifecycle through
     `*Async` overloads (create/bulk/search/get/index/delete/count), msearch + scroll async, scroll
     cursor to exhaustion with `ClearScroll`, `search_after` pagination via `Hit.Sort`, and the PIT
     lifecycle (`CreatePit` → search with `Pit` reference, no index on the request → `DeleteAllPits`).
  2b. [x] **Consumer-feedback ergonomics (Swisslex migration scorecard, 2026-07-01).** Three
     non-breaking fixes, each at the generator root:
     - `SourceConfigParam` (the `_source` query parameter on 15 requests, incl. `MgetRequest.Source`)
       was a raw `JsonElement` — and serialized field lists as raw JSON brackets in the query string.
       Now a hand-written bool-or-fields union (`Source = false` / `Source = new[] {"title"}`).
     - Expression-based field selection existed only for queries (the `<TDocument>` descriptor
       fixpoint was seeded with `QueryContainer`/`SpanQuery` only) — `DateHistogram`/`Histogram`
       and every other aggregation were string-only. The spec's `Field` schema is now tagged as a
       field path, the fixpoint is seeded with `AggregationContainer` and marks field-selecting
       types generic, and generic descriptors emit `Field(x => x.Prop)` overloads — 83 generic
       descriptors, plus a hand-written `AggregationsDictDescriptor<TDocument>` wired into
       `SearchRequestDescriptor<TDocument>.Aggregations` (filter aggregations get typed queries too).
     - Scalar-list descriptor setters generate single-value overloads (`Analyze(...).Text("hello")`,
       `Index("logs")`); the hand-written `AnalyzeRequestExtensions` wrapper is deleted.
     Remaining scorecard items are breaking and queued with item 3: root-namespace collision renames
     (`Entity`, `Filter`, ...) batched with the typed-`UpdateRequest` break.
  3. [x] **Breaking batch (typed update + collision renames)** — one break, not two.
     - **Typed `UpdateRequest<TDocument, TPartialDocument>`** (Java-blueprint shape): doc/upsert were
       raw `JsonElement`; now typed generic fields routed through the source serializer. Generator
       support is general (request-side type parameters; endpoints/client methods/descriptors span
       request+response parameters), not update-specific.
     - **Qualified schema renames.** The consumer-reported root-namespace collisions (`Entity`,
       `Filter`, `Operation`, `Source`, `Document`, `Node`) are renamed with domain prefixes
       (`AnomalyEntity`, `TermVectorsFilter`, `MgetOperation`/`MtermvectorsOperation`,
       `ReindexSource`/`MlSource`, `IngestDocument`, `MlNode`). Investigating them surfaced a real
       correctness bug: types dedupe by bare schema name, so same-named schemas from different spec
       files silently conflated into one C# type, first mapped wins — ml's model `Source` deserialized
       as reindex's `Source` (all fields lost), `IndexSettings`/`Status`/`Result`/`Metric` and more
       likewise. All 18 conflations are now disambiguated via a qualified (file+name) rename table,
       and the validator **fails generation** on any unrenamed same-name collision so a spec update
       can't reintroduce one silently.
  4. [x] Phase D package-identity decision — **keep `SB.*`** (see Phase D below).
  5. [ ] Validator-debt burn-down (19 empty / 26 stub / 9 fallback / 5 punts → fix or justify; ratchet).
  6. [ ] Benchmark baseline (C3).
- [ ] **C3.** A first performance/scale pass.

## Phase D — Package identity (decided 2026-07-02: keep `SB.*`)

- [x] **D1. Keep the `SB.` prefix.** The `OpenSearch.*` package-ID prefix is **reserved on
      nuget.org** by opensearch-project (their packages carry the verified badge), so any
      `OpenSearch.<x>` ID from another account is rejected at push — a "cleaner neutral name"
      would mean an unrelated prefix (e.g. `Osc.*`), which buys nothing over `SB.*` and costs
      existing consumers a deprecation + dual-publish migration.
- [x] **D2. Moot** — no rename, no migration path needed.

---

**Recommended order:** A → B → C, with D decided whenever the naming choice firms up.
Phase A first because it makes everything after it self-verifying and ends manual releases.
