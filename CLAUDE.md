# OpenSearch .NET Client v2 Rebuild

## Project Overview

This is a ground-up rebuild of the OpenSearch .NET client on the `v2` branch, using the opensearch-java client as the architectural blueprint. The `main` branch tracks upstream `opensearch-project/opensearch-net`.

**Goal:** A modern, spec-driven .NET client with System.Text.Json serialization, tagged unions for polymorphic types, and a code generator that produces typed client code from the OpenSearch API specification.

**Why rebuild instead of incrementally fixing:**
- The existing client is stuck in the NEST/elasticsearch-net v7 era
- Swapping Utf8Json for STJ in 607 coupled files + rewriting 83 hand-written formatters is actually more work than building a code generator
- The architecture (inheritance-based polymorphism, fat pipeline interfaces, 35+ config settings) is fundamentally behind the Java client's design
- Once a code generator exists, keeping up with OpenSearch releases becomes near-free

## Key Decisions

1. **Rebuild, not incremental** — New transport, new serialization, new types. Use Java client as blueprint.
2. **Fork structure** — `main` tracks upstream, `v2` is the rebuild. Fork relationship enables future upstream PRs.
3. **System.Text.Json only** — No Utf8Json, no migration shim. Optional Newtonsoft bridge for user document types.
4. **Target net8.0 + net9.0** — Clean break, no netstandard2.0.
5. **Code generator is the core investment** — Spec-driven generation from opensearch-api-specification, like Java does with java-codegen.
6. **Tagged unions for polymorphism** — `TaggedUnion<TKind, TValue>` with Kind enums, not inheritance hierarchies.

## Architecture (Java → C# Mapping)

| Java | C# |
|------|-----|
| `Transport` (4-method interface) | `IOpenSearchTransport` |
| `ApacheHttpClient5Transport` (~1100 lines) | `HttpClientTransport` (HttpClient + handler rotation for DNS refresh) |
| `Node` list + `ConcurrentHashMap<Host, DeadHostState>` | Same with `ConcurrentDictionary` + immutable `DeadNodeState` |
| `TransportOptions` | `TransportOptions` record |
| `JsonpMapper` | `IOpenSearchSerializer` |
| `JacksonJsonpMapper` | `SystemTextJsonSerializer` |
| `TaggedUnion<Kind, Value>` | `TaggedUnion<TKind, TValue>` |
| `JsonpSerializable` | STJ `[JsonConverter]` contracts |
| `NamedDeserializer` | `SourceConverter<T>` |
| `java-codegen` (13.6k lines, 60 Mustache) | `OpenSearch.CodeGen` (~8-12k lines, Scriban) |

### Patterns to use from elasticsearch-net v8:
- **ContextProvider<T>** — JsonConverterFactory that smuggles IConnectionSettings into STJ converters
- **Dual serializer** — Generated converters for API types, user-configurable serializer for document types
- **Marker type converters** — Per-property converter selection in generated code

### Good ideas from the old .NET transport (steal as patterns, don't reuse code):
- Handler rotation (`RequestDataHttpClientFactory`) for DNS refresh — genuinely better than Java
- Audit trail (diagnostic trace per request) — Java has nothing equivalent
- `RequestData` snapshot (freeze config per-request)

## Project Structure

```
src/
  OpenSearch.Net/              → Transport, nodes, connections, configuration
  OpenSearch.Client/           → Serialization infra, client, generated types
  OpenSearch.CodeGen/          → Code generator (console app, reads OpenAPI spec)
tests/
  OpenSearch.Net.Tests/
  OpenSearch.Client.Tests/
```

## Build Plan

### Phase 1: Transport + Serialization Foundation (weeks 1-2)
- Transport: IOpenSearchTransport, HttpClientTransport, node management, config, HTTP layer
- Serialization: IOpenSearchSerializer, SystemTextJsonSerializer, ContextProvider, TaggedUnion, ExternallyTaggedUnion, SourceConverter
- Build infra: Directory.Build.props, solution, projects, test projects

### Phase 2: Code Generator MVP (weeks 3-5)
- OpenAPI spec parser (adapt existing NSwag usage or build fresh)
- Shape model: ObjectShape, TaggedUnionShape, EnumShape, RequestShape
- Scriban templates for C# output
- Target: `indices` namespace end-to-end (103 operations, no NDJSON, no generics)

### Phase 3: Core API Coverage (weeks 5-8)
- Generics (SearchResponse<T>, Hit<T>), tagged unions (Query DSL, Aggregations, Mappings), typed keys
- `_core` namespace (search, get, index, delete)
- Hand-written NDJSON (bulk, msearch — even Java excludes these from codegen)

### Phase 4: App Migration + Polish (weeks 8-10)
- Additional namespaces as needed
- AWS SigV4 auth
- Integration tests against real cluster

### Phase 5: Upstream PRs
- PR the transport, codegen, and generated code back to opensearch-project/opensearch-net

## Key References

| Resource | URL |
|----------|-----|
| opensearch-java (architecture reference) | https://github.com/opensearch-project/opensearch-java |
| Java code generator | `java-codegen/` in opensearch-java repo |
| OpenSearch API specification | https://github.com/opensearch-project/opensearch-api-specification |
| Client Generator Guide | opensearch-api-specification/CLIENT_GENERATOR_GUIDE.md |
| elasticsearch-net v8 (STJ migration reference) | https://github.com/elastic/elasticsearch-net |
| Unified API Spec blog | https://opensearch.org/blog/revolutionizing-opensearch-clients-and-documentation-with-a-unified-api-specification/ |

### Key upstream issues:
- [Utf8Json → STJ (Issue #388)](https://github.com/opensearch-project/opensearch-net/issues/388) — open since Oct 2023, no PRs
- [OpenSearch 3.0 support (Issue #928)](https://github.com/opensearch-project/opensearch-net/issues/928) — no assignee
- ["Is this project dead?" (Issue #957)](https://github.com/opensearch-project/opensearch-net/issues/957)

## Existing Codebase Analysis (for reference)

### Utf8Json coupling in current code:
- 686 files reference Utf8Json, 607 in OpenSearch.Client
- 83 custom formatters, 377 `[JsonFormatter]` annotations, 3,433 `[DataMember]` annotations
- 57 vendored Utf8Json files (20,539 lines of abandoned code)

### OpenAPI spec stats:
- 480 unique API methods, ~1,975 schemas, 66,245 lines YAML
- Custom extensions: x-operation-group, x-ignorable, x-overloaded-param, x-ndjson, x-is-generic-type-parameter, x-supports-typed-keys
- 4 NDJSON endpoints (must be hand-written): bulk, bulk_stream, msearch, msearch_template
- Java codegen excludes ~20 namespaces it can't yet generate

### Transport comparison (old .NET vs Java):
- Old .NET: fat IRequestPipeline (20+ members), 6 pool implementations, 35+ config settings, Node thread-safety bug, dead code
- Java: single transport class, node list + concurrent denylist, ~12 settings, immutable DeadHostState

### gRPC future:
- OpenSearch 3.2+ has gRPC for Bulk/kNN (10-200% perf improvement)
- Transport abstraction should keep this door open
