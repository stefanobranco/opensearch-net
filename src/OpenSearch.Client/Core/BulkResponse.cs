using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Client.Core;

/// <summary>
/// Response from the bulk API.
/// </summary>
public sealed class BulkResponse : OpenSearchResponse
{
	/// <summary>How long the operation took, in milliseconds.</summary>
	public long Took { get; set; }

	/// <summary>Whether any of the operations had errors.</summary>
	public bool Errors { get; set; }

	/// <summary>The results of each operation, in order.</summary>
	public List<BulkResponseItem>? Items { get; set; }

	/// <summary>
	/// Whether this response represents a fully successful bulk operation.
	/// Returns <c>false</c> when any individual operation had errors, even if the
	/// HTTP call itself succeeded.
	/// </summary>
	[JsonIgnore]
	public override bool IsValid => base.IsValid && !Errors;

	/// <summary>Returns items that had errors (status >= 400).</summary>
	public IEnumerable<BulkResponseItem> ItemsWithErrors =>
		Items?.Where(i =>
		{
			var result = i.Index ?? i.Create ?? i.Update ?? i.Delete;
			return result is not null && result.Status >= 400;
		}) ?? [];
}

/// <summary>
/// The result of a single bulk operation.
/// </summary>
public sealed class BulkResponseItem
{
	/// <summary>The index operation result, if this was an index operation.</summary>
	public BulkResponseItemResult? Index { get; set; }

	/// <summary>The create operation result, if this was a create operation.</summary>
	public BulkResponseItemResult? Create { get; set; }

	/// <summary>The update operation result, if this was an update operation.</summary>
	public BulkResponseItemResult? Update { get; set; }

	/// <summary>The delete operation result, if this was a delete operation.</summary>
	public BulkResponseItemResult? Delete { get; set; }
}

/// <summary>
/// The result details of a single bulk operation.
/// </summary>
public sealed class BulkResponseItemResult
{
	[JsonPropertyName("_index")]
	public string? Index { get; set; }

	[JsonPropertyName("_id")]
	public string? Id { get; set; }

	[JsonPropertyName("_version")]
	public long? Version { get; set; }

	public string? Result { get; set; }

	[JsonPropertyName("_shards")]
	public JsonElement? Shards { get; set; }

	[JsonPropertyName("_seq_no")]
	public long? SeqNo { get; set; }

	[JsonPropertyName("_primary_term")]
	public long? PrimaryTerm { get; set; }

	public int Status { get; set; }

	public JsonElement? Error { get; set; }
}
