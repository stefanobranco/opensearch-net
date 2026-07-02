using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Request for the bulk API. Contains a list of <see cref="BulkOperation"/> items
/// that are serialized as NDJSON (Newline Delimited JSON).
/// </summary>
public sealed class BulkRequest
{
	/// <summary>Default index for operations that don't specify one.</summary>
	[JsonIgnore]
	public string? Index { get; set; }

	/// <summary>The pipeline ID for preprocessing documents.</summary>
	[JsonIgnore]
	public string? Pipeline { get; set; }

	/// <summary>Whether to refresh the affected shards. Defaults to false.</summary>
	[JsonIgnore]
	public string? Refresh { get; set; }

	/// <summary>The routing value for all operations.</summary>
	[JsonIgnore]
	public string? Routing { get; set; }

	/// <summary>Timeout for the request.</summary>
	[JsonIgnore]
	public string? Timeout { get; set; }

	/// <summary>Whether to require an alias for the target index.</summary>
	[JsonIgnore]
	public bool? RequireAlias { get; set; }

	/// <summary>The bulk operations to execute.</summary>
	[JsonIgnore]
	public List<BulkOperation> Operations { get; set; } = [];
}

/// <summary>
/// Base class for bulk operations (index, create, update, delete).
/// </summary>
public abstract class BulkOperation
{
	/// <summary>Writes the action line (e.g., { "index": { "_index": "...", "_id": "..." } }).</summary>
	internal abstract void WriteAction(Utf8JsonWriter writer);

	/// <summary>Whether this operation is followed by a body line (false for delete).</summary>
	internal abstract bool HasBody { get; }

	/// <summary>Writes the body line. Only called when <see cref="HasBody"/> is true.</summary>
	internal virtual void WriteBody(Utf8JsonWriter writer, JsonSerializerOptions options)
	{
	}
}

/// <summary>
/// A bulk index operation. Indexes a document (creates or replaces).
/// </summary>
public sealed class BulkIndexOperation<TDocument> : BulkOperation
{
	/// <summary>The document to index.</summary>
	public required TDocument Document { get; init; }

	/// <summary>The target index. Overrides the bulk request's default index.</summary>
	public string? Index { get; init; }

	/// <summary>The document ID.</summary>
	public string? Id { get; init; }

	/// <summary>The routing value.</summary>
	public string? Routing { get; init; }

	/// <summary>The pipeline ID.</summary>
	public string? Pipeline { get; init; }

	internal override void WriteAction(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteStartObject("index"u8);
		if (Index is not null) writer.WriteString("_index"u8, Index);
		if (Id is not null) writer.WriteString("_id"u8, Id);
		if (Routing is not null) writer.WriteString("routing"u8, Routing);
		if (Pipeline is not null) writer.WriteString("pipeline"u8, Pipeline);
		writer.WriteEndObject();
		writer.WriteEndObject();
	}

	internal override bool HasBody => Document is not null;

	internal override void WriteBody(Utf8JsonWriter writer, JsonSerializerOptions options) =>
		SourceDocument.WriteDocument(writer, Document!, options);
}

/// <summary>
/// A bulk create operation. Creates a document (fails if it already exists).
/// </summary>
public sealed class BulkCreateOperation<TDocument> : BulkOperation
{
	/// <summary>The document to create.</summary>
	public required TDocument Document { get; init; }

	/// <summary>The target index.</summary>
	public string? Index { get; init; }

	/// <summary>The document ID.</summary>
	public string? Id { get; init; }

	/// <summary>The routing value.</summary>
	public string? Routing { get; init; }

	/// <summary>The pipeline ID.</summary>
	public string? Pipeline { get; init; }

	internal override void WriteAction(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteStartObject("create"u8);
		if (Index is not null) writer.WriteString("_index"u8, Index);
		if (Id is not null) writer.WriteString("_id"u8, Id);
		if (Routing is not null) writer.WriteString("routing"u8, Routing);
		if (Pipeline is not null) writer.WriteString("pipeline"u8, Pipeline);
		writer.WriteEndObject();
		writer.WriteEndObject();
	}

	internal override bool HasBody => Document is not null;

	internal override void WriteBody(Utf8JsonWriter writer, JsonSerializerOptions options) =>
		SourceDocument.WriteDocument(writer, Document!, options);
}

/// <summary>
/// A bulk update operation. Updates an existing document.
/// </summary>
public sealed class BulkUpdateOperation<TDocument> : BulkOperation
{
	/// <summary>The partial document or script for the update.</summary>
	public TDocument? Doc { get; init; }

	/// <summary>Whether to upsert (insert if not exists).</summary>
	public bool? DocAsUpsert { get; init; }

	/// <summary>The target index.</summary>
	public string? Index { get; init; }

	/// <summary>The document ID.</summary>
	public required string Id { get; init; }

	/// <summary>The routing value.</summary>
	public string? Routing { get; init; }

	/// <summary>Number of retries on conflict.</summary>
	public int? RetryOnConflict { get; init; }

	internal override void WriteAction(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteStartObject("update"u8);
		if (Index is not null) writer.WriteString("_index"u8, Index);
		writer.WriteString("_id"u8, Id);
		if (Routing is not null) writer.WriteString("routing"u8, Routing);
		if (RetryOnConflict is not null) writer.WriteNumber("retry_on_conflict"u8, RetryOnConflict.Value);
		writer.WriteEndObject();
		writer.WriteEndObject();
	}

	internal override bool HasBody => true;

	internal override void WriteBody(Utf8JsonWriter writer, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		if (Doc is not null)
		{
			writer.WritePropertyName("doc"u8);
			SourceDocument.WriteDocument(writer, Doc, options);
		}
		if (DocAsUpsert is not null) writer.WriteBoolean("doc_as_upsert"u8, DocAsUpsert.Value);
		writer.WriteEndObject();
	}
}

/// <summary>
/// A bulk delete operation. Deletes an existing document.
/// </summary>
public sealed class BulkDeleteOperation : BulkOperation
{
	/// <summary>The target index.</summary>
	public string? Index { get; init; }

	/// <summary>The document ID to delete.</summary>
	public required string Id { get; init; }

	/// <summary>The routing value.</summary>
	public string? Routing { get; init; }

	internal override void WriteAction(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteStartObject("delete"u8);
		if (Index is not null) writer.WriteString("_index"u8, Index);
		writer.WriteString("_id"u8, Id);
		if (Routing is not null) writer.WriteString("routing"u8, Routing);
		writer.WriteEndObject();
		writer.WriteEndObject();
	}

	internal override bool HasBody => false;
}
