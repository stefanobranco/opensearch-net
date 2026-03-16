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

### 2. Serialization Pipeline
- [ ] Every `SerializeToElement` call — does it pass snake_case options?
- [ ] Every `JsonSerializer.Deserialize` call outside the transport — correct options?
- [ ] Generated types — relying on naming policy vs explicit `[JsonPropertyName]`?
- [ ] `OpenSearchClientSettings` serializer setup — options complete?
- [ ] `SystemTextJsonSerializer` — serialize/deserialize paths
- [ ] `SourceConverter` — user document type serialization
- [ ] Enum serialization — `JsonEnumConverterFactory`, snake_case enum values
- [ ] Compare against elasticsearch-net `DefaultRequestResponseSerializer`

### 3. Response Model
- [ ] `OpenSearchResponse` base class — `IsValid`, `ServerError`, `ApiCall`, `DebugInformation`
- [ ] `SearchResponse<T>` — `IsValid` override for shard failures
- [ ] `BulkResponse` — `ItemsWithErrors`, error detection
- [ ] `MsearchResponse` — `GetResponses<T>()`, typed item handling
- [ ] `MgetResponse` — `MgetResponseItem`, `GetDocs<T>()`, `GetMany<T>()`
- [ ] `DeleteByQueryResponse` — all fields present
- [ ] `DebugInformation` formatting — complete and useful?
- [ ] Compare against elasticsearch-net `ElasticsearchResponse`

### 4. Aggregations
- [ ] `AggregateDictionary` — all metric accessors (avg, sum, min, max, cardinality, stats, extended_stats)
- [ ] Bucket accessors — terms, date_histogram, histogram, range, composite, significant_terms
- [ ] Single-bucket aggs — filter, nested, reverse_nested, global, sampler
- [ ] Sub-aggregation parsing — `ParseSubAggregations`, `ExtensionData` flow
- [ ] `BucketAggregate<T>` — IReadOnlyList implementation, `.Buckets` accessor
- [ ] Bucket sub-agg convenience accessors (TermsBucket.Terms(), .Filter(), etc.)
- [ ] `typed_keys` handling — prefix stripping
- [ ] Missing agg types? (percentiles, percentile_ranks, geo_bounds, top_hits, scripted_metric, etc.)
- [ ] Compare against elasticsearch-net `AggregateDictionary`

### 5. Query DSL & Descriptors
- [ ] `QueryContainerDescriptor<T>` — all query types available?
- [ ] `QueryContainerDescriptor` (non-generic) — same coverage?
- [ ] Field expression resolution — `FieldExpressionVisitor`, property name → field name
- [ ] Tagged union serialization — `ExternallyTaggedUnion`, `InternallyTaggedUnion`
- [ ] Sort options — `SortOptions`, `SortOrder`, field sort, score sort, script sort
- [ ] Source filtering — `SourceConfig`, include/exclude
- [ ] Highlight — `HighlightDescriptor`, field highlights
- [ ] `IntervalsQuery` overloads — all interval types
- [ ] Compare against elasticsearch-net query descriptors

### 6. Bulk & NDJSON
- [ ] `BulkRequest` serialization — NDJSON format correct?
- [ ] `BulkResponse` deserialization — items, errors, `ItemsWithErrors`
- [ ] `BulkIndexOperation<T>`, `BulkDeleteOperation`, `BulkUpdateOperation<T>` — all operation types
- [ ] `MsearchRequest` — NDJSON header + body serialization
- [ ] `MsearchResponse` — deserialization of multiple search responses
- [ ] `MsearchRequestExtensions.AddSearch` — body field mapping complete?
- [ ] Compare against elasticsearch-net bulk/msearch handling

### 7. Search Response Chain
- [ ] `SearchResponse<T>` — all fields present (hits, aggregations, suggest, _scroll_id, pit_id, etc.)
- [ ] `Hit<T>` — `_source`, `_id`, `_index`, `_score`, `highlight`, `inner_hits`, `fields`, `sort`
- [ ] `HitsMetadata<T>` — `Total`, `MaxScore`, `Hits` list
- [ ] `TotalHits` — `Value`, `Relation` (eq/gte)
- [ ] `SuggestDictionary<T>` — term, phrase, completion suggest parsing
- [ ] `SuggestEntry<T>` — options, text, offset, length
- [ ] Scroll responses — `_scroll_id` preserved, `Documents()` extension
- [ ] `InnerHits` — deserialization, typed access
- [ ] Compare against elasticsearch-net search response types

### 8. Convenience API & NEST Compat
- [ ] `OpenSearchClient` root methods — `Index<T>`, `IndexMany<T>`, `Get`, `Delete`, `DeleteByQuery`
- [ ] `Scroll<T>`, `ClearScroll` — full scroll lifecycle
- [ ] `MultiSearch`, `MultiGet` aliases
- [ ] CancellationToken overloads on all async methods
- [ ] `RefreshIndexRequest` usage — is it ergonomic?
- [ ] Missing convenience methods consumers need?
- [ ] Compare against NEST client API surface

### 9. Generated Code Quality
- [ ] Spot-check 10-20 generated types against OpenAPI spec — fields match?
- [ ] Generated descriptors — do they expose all fields?
- [ ] Generated endpoints — HTTP method, URL pattern, body serialization
- [ ] Generated responses — inherit `OpenSearchResponse`?
- [ ] Generated enums — values match spec, serialization correct?
- [ ] `[JsonPropertyName]` vs naming policy — consistent strategy?
- [ ] Compare generated output against opensearch-java codegen output

### 10. Test Coverage
- [ ] Transport tests — all paths covered (success, 4xx, 5xx, retries, timeouts)?
- [ ] Serialization tests — round-trip for key types
- [ ] Aggregation tests — all accessor methods, sub-agg parsing
- [ ] Query DSL tests — serialization of all query types
- [ ] Bulk/NDJSON tests — format validation
- [ ] Search response tests — deserialization of full responses
- [ ] Diagnostics tests — `DebugInformation`, `ServerError` formatting
- [ ] Integration tests — coverage vs unit tests
