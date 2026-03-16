# v2 Client Review Checklist

Comprehensive review areas for the opensearch-net v2 rebuild before it's PR-ready, comparing against elasticsearch-net v8 and opensearch-java reference clients.

Work through areas one at a time. For each: read all relevant files, compare against reference clients, fix issues found, add tests for gaps.

## Areas

### 1. Transport & Error Handling ✅
- [x] `HttpClientTransport` — retry logic, node selection, dead-node tracking
  - Fixed: MarkAlive on retryable status final attempt (now MarkDead + fall-through to server error handling)
  - Fixed: retryable status on final attempt respects ThrowExceptions=false (returns response, not always throws)
  - Verified: cancellation correctly propagates as OperationCanceledException, never wrapped
- [x] `ServerError` parsing — all error shapes (string error, object error, nested caused_by)
  - Correct: handles JSON object, JSON string, non-JSON bodies gracefully
- [x] `ThrowExceptions` path — opt-in throwing vs default non-throwing
  - Correct: server errors + retryable-status-exhausted respect the flag; connection failures always throw (no response available)
- [x] `IsServerError` — 4xx/5xx classification, HEAD/GET/DELETE 404 special cases
  - Fixed: GET/DELETE/HEAD 404 now Success=true, IsValid=true (was incorrectly false)
- [x] `TransportException` — retries exhausted, connection failures
  - Fixed: ApiCallDetails now includes correct HttpMethod and Uri (was default GET / null)
- [x] `ApiCallDetails` population — all fields populated correctly on all paths
  - Error bytes always captured; success bytes only when DisableDirectStreaming
- [x] Compare against elasticsearch-net `HttpTransport` and opensearch-java `ApacheHttpClient5Transport`
  - Architecture aligns with Java blueprint; improvements: handler rotation for DNS, audit trail, zero-alloc DeadNodeState
  - Noted gap: no NodeSelector abstraction for filtering by role (future work)

### 2. Serialization Pipeline ✅
- [x] Every `SerializeToElement` call — does it pass snake_case options?
  - Verified: all calls serialize primitives, arrays, or dictionaries with wire-format keys — naming policy is irrelevant
  - `AggregationsDictDescriptor`, `SearchRequestDescriptorExtensions` correctly use `OpenSearchJsonOptions.RequestSerialization`
- [x] Every `JsonSerializer.Deserialize` call outside the transport — correct options?
  - All pass either `options` (from converter context), `OpenSearchJsonOptions.Default`, or `RequestSerialization`
- [x] Generated types — relying on naming policy vs explicit `[JsonPropertyName]`?
  - Strategy: naming policy for standard PascalCase→snake_case, `[JsonPropertyName]` only for underscore-prefixed or non-standard fields (correct)
- [x] `OpenSearchClientSettings` serializer setup — options complete?
  - Correct: snake_case, WhenWritingNull, AllowReadingFromString, 3 converters (ContextProvider, JsonEnumConverterFactory, ServerErrorConverter)
- [x] `SystemTextJsonSerializer` — serialize/deserialize paths
  - Clean wrapper, handles empty streams, MemoryStream TryGetBuffer optimization
- [x] `SourceConverter` — user document type serialization
  - Correct delegation via ContextProvider. Note: not yet wired to generated types (no `[JsonConverter]` on Hit.Source etc.) — custom SourceSerializer won't work for search hits yet (codegen gap, default case works)
- [x] Enum serialization — `JsonEnumConverterFactory`, snake_case enum values
  - `[JsonEnum]` + `[EnumMember(Value)]` pattern, case-insensitive read. `QueryParamSerializer` for query strings. Both correct.
  - Fixed: added `JsonEnumConverterFactory` to `OpenSearchJsonOptions.Default` (prophylactic — prevents future silent failures when response fragments include enum fields)
- [x] Compare against elasticsearch-net `DefaultRequestResponseSerializer`
  - Mirrors Elastic v8 architecture: ContextProvider, dual serializer, JsonEnumConverterFactory, ServerErrorConverter
  - Gap vs Elastic: SourceConverter not wired to generated types via `[JsonConverter]` attributes (future codegen work)

### 3. Response Model ✅
- [x] `OpenSearchResponse` base class — `IsValid`, `ServerError`, `ApiCall`, `DebugInformation`
  - Correct: virtual IsValid checks ApiCall.Success && ServerError is null; manually constructed responses (null ApiCall) default to valid
  - All properties `[JsonIgnore]` since they're populated by transport, not deserialized
- [x] `SearchResponse<T>` — `IsValid` override for shard failures
  - Correct: overrides to `base.IsValid && (Shards is null || Shards.Failed == 0)`
  - Extension methods: Documents(), Total(), Aggs(), Suggestions()
- [x] `BulkResponse` — `ItemsWithErrors`, error detection
  - Fixed: added `IsValid` override (`base.IsValid && !Errors`) — HTTP 200 with individual item failures was incorrectly returning IsValid=true
  - `ItemsWithErrors` filters status >= 400 across Index/Create/Update/Delete operations
- [x] `MsearchResponse` — `GetResponses<T>()`, typed item handling
  - Correct: deserializes each item's hits, aggregations, suggest with optional custom options
  - `MsearchTypedResponse<T>.IsValid` checks status + no error
- [x] `MgetResponse` — `MgetResponseItem`, `GetDocs<T>()`, `GetMany<T>()`
  - Correct: typed `MgetHit<T>` with all fields, `GetMany` preserves ID order (NEST-compatible)
- [x] `DeleteByQueryResponse` — all fields present
  - All fields present. Note: `Failures` typed as `List<object>?` (raw JSON) — could be typed in future codegen
  - Generated (sealed, not partial) — can't add IsValid override without codegen changes
- [x] `DebugInformation` formatting — complete and useful?
  - Rich: includes method/URI, audit trail (per-node events with timing/status), request/response bodies, original exception
  - SearchResponse adds shard failure details via SearchDebugInformation()
- [x] Compare against elasticsearch-net `ElasticsearchResponse`
  - Closely mirrors architecture. Key difference: abstract class vs interface. Bulk/Search IsValid overrides match.
  - 314 generated response types all properly inherit from OpenSearchResponse

### 4. Aggregations ✅
- [x] `AggregateDictionary` — all metric accessors (avg, sum, min, max, cardinality, stats, extended_stats)
  - All common single-value metrics present: Average, Sum, Min, Max, Cardinality, ValueCount, Stats, ExtendedStats
- [x] Bucket accessors — terms, date_histogram, histogram, range, composite, significant_terms
  - All 6 multi-bucket types present with typed bucket classes and sub-agg parsing
- [x] Single-bucket aggs — filter, nested, reverse_nested, global, sampler
  - Added: ReverseNested() and Global() accessors with ReverseNestedBucket/GlobalBucket types
  - Expanded IsSingleBucketKind to include "children" and "sampler"
- [x] Sub-aggregation parsing — `ParseSubAggregations`, `ExtensionData` flow
  - Correct: multi-bucket via unknown-property collection, single-bucket via ExtensionData
  - IBucketWithSubAggregations interface for clean injection
- [x] `BucketAggregate<T>` — IReadOnlyList implementation, `.Buckets` accessor
  - Correct: implements IReadOnlyList<T>, indexing, enumeration, LINQ
- [x] Bucket sub-agg convenience accessors (TermsBucket.Terms(), .Filter(), etc.)
  - Added: consistent convenience accessors (Terms, Filter, Average, Sum, Min, Max, Cardinality) to all bucket types
  - Previously only TermsBucket and NestedBucket had them
- [x] `typed_keys` handling — prefix stripping
  - Correct: StripTypedKeys parses "type#name" format, stores discriminator in separate dict
- [x] Missing agg types? (percentiles, percentile_ranks, geo_bounds, top_hits, scripted_metric, etc.)
  - Descriptor covers 42 agg types (20 bucket, 16 metric, 6 pipeline). Response accessors cover common ones.
  - Noted gap: percentiles, geo_bounds, top_hits, scripted_metric lack typed response accessors (use GetRaw() + manual parsing). Future work — Aggregate<T> already has the raw fields.
- [x] Compare against elasticsearch-net `AggregateDictionary`
  - Same pattern: typed accessors for common aggs, extensible architecture. Trade-off: smaller API surface (easier to maintain) vs less type-safe for advanced aggs.

### 5. Query DSL & Descriptors ✅
- [x] `QueryContainerDescriptor<T>` — all query types available?
  - 55 query types with expression-based field selection, dual overloads (expression + string), implicit conversion to QueryContainer
- [x] `QueryContainerDescriptor` (non-generic) — same coverage?
  - Auto-generated, 55 query types, dual builder methods (direct + fluent), field-keyed queries as Dictionary<string, T>
- [x] Field expression resolution — `FieldExpressionVisitor`, property name → field name
  - Per-member caching, [JsonPropertyName] attribute-first, snake_case fallback, nested property chains, .Suffix() support
- [x] Tagged union serialization — `ExternallyTaggedUnion`, `InternallyTaggedUnion`
  - QueryContainer uses externally-tagged format with QueryContainerConverter (bidirectional kind/name mapping)
  - InternallyTaggedUnionConverter for discriminator-inside-object types (Property, etc.)
- [x] Sort options — `SortOptions`, `SortOrder`, field sort, score sort, script sort
  - Polymorphic: Field, Score, Doc, GeoDistance, Script. SortOptionsConverter handles all wire formats including shorthands.
- [x] Source filtering — `SourceConfig`, include/exclude
  - Bool/filter polymorphism with SourceConfigConverter, implicit operator from bool
- [x] Highlight — `HighlightDescriptor`, field highlights
  - Full highlight support with per-field overrides, query-based highlighting, all options
- [x] `IntervalsQuery` overloads — all interval types
  - AllOf, AnyOf, Fuzzy, Match, Prefix, Wildcard with descriptors and field-keyed support
- [x] Compare against elasticsearch-net query descriptors
  - Closely mirrors v8 patterns. Same dual descriptor approach, same field resolution, same tagged union architecture.

### 6. Bulk & NDJSON ✅
- [x] `BulkRequest` serialization — NDJSON format correct?
  - Correct: NdjsonWriter streams action + optional body lines per operation, zero-copy via RequestBody.Custom
- [x] `BulkResponse` deserialization — items, errors, `ItemsWithErrors`
  - Fixed in Area 3: IsValid override for Errors=true. ItemsWithErrors filters status >= 400 across all op types.
- [x] `BulkIndexOperation<T>`, `BulkDeleteOperation`, `BulkUpdateOperation<T>` — all operation types
  - All 4 types (Index, Create, Update, Delete) correct. Update supports Doc + DocAsUpsert (script/upsert fields future work).
- [x] `MsearchRequest` — NDJSON header + body serialization
  - Correct: MsearchEndpoint uses NdjsonWriter.WriteMsearch, header + body pairs, application/x-ndjson content type
- [x] `MsearchResponse` — deserialization of multiple search responses
  - Correct: GetResponses<T>() with typed hits, aggregations, suggest per item
- [x] `MsearchRequestExtensions.AddSearch` — body field mapping complete?
  - Comprehensive: 25+ fields mapped including query, aggregations, sort, source, highlight, collapse, knn, suggest, etc.
- [x] Compare against elasticsearch-net bulk/msearch handling
  - Cleaner design: RequestBody abstraction + NdjsonWriter vs raw byte streams. Same wire format.

### 7. Search Response Chain ✅
- [x] `SearchResponse<T>` — all fields present (hits, aggregations, suggest, _scroll_id, pit_id, etc.)
  - 14 fields including Took, TimedOut, Shards, Hits, Aggregations, Suggest, ScrollId, PitId, Profile, Clusters, TerminatedEarly, NumReducePhases, PhaseTook, ProcessorResults
- [x] `Hit<T>` — `_source`, `_id`, `_index`, `_score`, `highlight`, `inner_hits`, `fields`, `sort`
  - 20+ fields with explicit [JsonPropertyName] for underscore-prefixed fields, [JsonExtensionData] for extensibility
- [x] `HitsMetadata<T>` — `Total`, `MaxScore`, `Hits` list
  - Correct, with LINQ extensions (Select, Where, FirstOrDefault, Count)
- [x] `TotalHits` — `Value`, `Relation` (eq/gte)
  - Custom TotalHitsConverter handles both integer and object wire formats. Implicit operator to long.
- [x] `SuggestDictionary<T>` — term, phrase, completion suggest parsing
  - Typed accessors: GetTerm(), GetPhrase(), GetCompletion() with proper option types
- [x] `SuggestEntry<T>` — options, text, offset, length
  - TermSuggestOption, PhraseSuggestOption, CompletionSuggestOption<T> all present
- [x] Scroll responses — `_scroll_id` preserved, `Documents()` extension
  - ScrollResponse<T> mirrors SearchResponse, preserves ScrollId, has Documents()/Total() extensions
- [x] `InnerHits` — deserialization, typed access
  - InnerHitsResult<T> with full HitsMetadata<T>, typed access via hit.InnerHits["name"]
- [x] Compare against elasticsearch-net search response types
  - Mirrors v8 architecture with STJ. Same extension pattern (Documents, Total, Aggs, Suggestions).

### 8. Convenience API & NEST Compat ✅
- [x] `OpenSearchClient` root methods — `Index<T>`, `IndexMany<T>`, `Get`, `Delete`, `DeleteByQuery`
  - Root shortcuts: Search, Index, Get, Delete, Bulk, Mget, Msearch, Scroll (all sync+async)
  - Noted gap: Create, Update, ClearScroll, DeleteByQuery only via client.Core namespace
  - Noted gap: IndexMany<T> convenience wrapper not implemented (users build BulkRequest)
- [x] `Scroll<T>`, `ClearScroll` — full scroll lifecycle
  - Scroll<T> at root, ClearScroll via client.Core.ClearScroll(). ScrollResponse preserves _scroll_id.
- [x] `MultiSearch`, `MultiGet` aliases
  - Abbreviated names Msearch/Mget used (no MultiSearch/MultiGet aliases). Matches spec naming.
- [x] CancellationToken overloads on all async methods
  - All async methods accept CancellationToken with default value
- [x] `RefreshIndexRequest` usage — is it ergonomic?
  - Available via client.Indices.Refresh() with fluent descriptor. No root-level shortcut.
- [x] Missing convenience methods consumers need?
  - Future work: IndexMany<T>, root shortcuts for Create/Update/DeleteByQuery/ClearScroll/Count/Exists
- [x] Compare against NEST client API surface
  - Core operations present. NEST had more root-level shortcuts; v2 uses namespace pattern (client.Core, client.Indices) for discoverability.

### 9. Generated Code Quality ✅
- [x] Spot-check 10-20 generated types against OpenAPI spec — fields match?
  - Checked 20+ types across Common, Cluster, Indices, Core namespaces. Fields match spec, proper nullable types, doc comments.
- [x] Generated descriptors — do they expose all fields?
  - All fields exposed as fluent builder methods with chaining. Implicit conversion to underlying request type.
- [x] Generated endpoints — HTTP method, URL pattern, body serialization
  - HTTP methods correct (GET/POST), URL patterns with dynamic segments, query param encoding via Uri.EscapeDataString, body via RequestBody.Json
- [x] Generated responses — inherit `OpenSearchResponse`?
  - All inherit OpenSearchResponse. Polymorphic types handled (Dictionary-based responses).
- [x] Generated enums — values match spec, serialization correct?
  - Consistent [JsonEnum] + [EnumMember(Value)] pattern. Wire values match spec (snake_case).
- [x] `[JsonPropertyName]` vs naming policy — consistent strategy?
  - Optimal: underscore-prefixed/camelCase get explicit attributes, standard PascalCase relies on naming policy. Consistent across all generated types.
- [x] Compare generated output against opensearch-java codegen output
  - Same tagged union approach, same property mapping strategy. C# uses STJ naming policy where Java uses Jackson annotations.

### 10. Test Coverage ✅
- [x] Transport tests — all paths covered (success, 4xx, 5xx, retries, timeouts)?
  - 37 tests: success, all error codes, retries, dead nodes, cancellation, timeout vs user cancellation, auth headers, compression
- [x] Serialization tests — round-trip for key types
  - 170+ tests: SystemTextJsonSerializer, query DSL, search requests/responses, enums, converters, tagged unions
- [x] Aggregation tests — all accessor methods, sub-agg parsing
  - 21 tests: all metric accessors, all bucket types, sub-agg parsing, typed_keys stripping
- [x] Query DSL tests — serialization of all query types
  - 22 unit + 5 integration: MatchAll, Match, Term, Terms, Range, Bool, Exists, Prefix, Wildcard, Fuzzy
- [x] Bulk/NDJSON tests — format validation
  - 11 tests: response deserialization, mixed operations, IsValid, ItemsWithErrors. Noted gap: no NDJSON write-format verification test.
- [x] Search response tests — deserialization of full responses
  - 17 tests: Documents(), Total(), Aggs(), Suggestions(), InnerHits, ScrollId, TotalHits converter
- [x] Diagnostics tests — `DebugInformation`, `ServerError` formatting
  - 28 tests: ApiCallDetails attachment, DebugInformation format, ServerError parsing (all shapes), error chain traversal
- [x] Integration tests — coverage vs unit tests
  - 48 integration tests across 19 files. Core operations, query DSL, cluster/indices admin, error handling. Unit tests cover serialization/transport depth; integration tests validate end-to-end.
