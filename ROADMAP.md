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
- [ ] **C3.** A first performance/scale pass.

## Phase D — Package identity (open decision, not yet actioned)

- [ ] **D1.** Decide whether to drop the `SB.` prefix for a cleaner neutral name.
      Constraint: `OpenSearch.Client` / `OpenSearch.Net` are owned by opensearch-project and are
      **unavailable** — this means a *different* neutral prefix, not the official IDs.
- [ ] **D2.** If renaming: migration path — publish under the new ID, mark the old `SB.*` packages
      deprecated, dual-publish for a transition window so existing consumers don't break.

---

**Recommended order:** A → B → C, with D decided whenever the naming choice firms up.
Phase A first because it makes everything after it self-verifying and ends manual releases.
