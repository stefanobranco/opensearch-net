using System.Text.Json.Serialization;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Sort by a document field. The <see cref="FieldName"/> becomes the JSON key,
/// and all other properties are serialized as the value object.
/// Wire format: <c>{"price": {"order": "asc", "mode": "avg"}}</c>
/// </summary>
public sealed class FieldSort
{
	/// <summary>The field name to sort by. Becomes the JSON property key (not serialized as a property).</summary>
	[JsonIgnore]
	public string FieldName { get; set; } = "";

	/// <summary>The sort order direction.</summary>
	public SortOrder? Order { get; set; }

	/// <summary>The mode for sorting on array fields.</summary>
	public string? Mode { get; set; }

	/// <summary>The value to use when the field is missing.</summary>
	public System.Text.Json.JsonElement? Missing { get; set; }

	/// <summary>The nested path sort options.</summary>
	public NestedSortValue? Nested { get; set; }

	/// <summary>The type to use for unmapped fields.</summary>
	public FieldType? UnmappedType { get; set; }

	/// <summary>The numeric type to use for sorting.</summary>
	public string? NumericType { get; set; }
}
