using System.Text.Json.Serialization;

namespace OpenSearch.Client.Core;

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
	/// <summary>Returns the action object (e.g., { "index": { "_index": "...", "_id": "..." } }).</summary>
	internal abstract object GetActionObject();

	/// <summary>Returns the document body, or null for delete operations.</summary>
	internal abstract object? GetBody();
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

	internal override object GetActionObject()
	{
		var meta = new Dictionary<string, object?>();
		if (Index is not null) meta["_index"] = Index;
		if (Id is not null) meta["_id"] = Id;
		if (Routing is not null) meta["routing"] = Routing;
		if (Pipeline is not null) meta["pipeline"] = Pipeline;
		return new Dictionary<string, object> { ["index"] = meta };
	}

	internal override object? GetBody() => Document;
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

	internal override object GetActionObject()
	{
		var meta = new Dictionary<string, object?>();
		if (Index is not null) meta["_index"] = Index;
		if (Id is not null) meta["_id"] = Id;
		if (Routing is not null) meta["routing"] = Routing;
		if (Pipeline is not null) meta["pipeline"] = Pipeline;
		return new Dictionary<string, object> { ["create"] = meta };
	}

	internal override object? GetBody() => Document;
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

	internal override object GetActionObject()
	{
		var meta = new Dictionary<string, object?>();
		if (Index is not null) meta["_index"] = Index;
		meta["_id"] = Id;
		if (Routing is not null) meta["routing"] = Routing;
		if (RetryOnConflict is not null) meta["retry_on_conflict"] = RetryOnConflict;
		return new Dictionary<string, object> { ["update"] = meta };
	}

	internal override object? GetBody()
	{
		var body = new Dictionary<string, object?>();
		if (Doc is not null) body["doc"] = Doc;
		if (DocAsUpsert is not null) body["doc_as_upsert"] = DocAsUpsert;
		return body;
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

	internal override object GetActionObject()
	{
		var meta = new Dictionary<string, object?>();
		if (Index is not null) meta["_index"] = Index;
		meta["_id"] = Id;
		if (Routing is not null) meta["routing"] = Routing;
		return new Dictionary<string, object> { ["delete"] = meta };
	}

	internal override object? GetBody() => null;
}
