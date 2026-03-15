using System.Text.Json.Serialization;
using OpenSearch.Client.Core;

namespace OpenSearch.Client;

/// <summary>
/// Represents a sort option in a search request. Can be a field sort (by field name),
/// score sort, doc sort, geo distance sort, or script sort.
/// </summary>
[JsonConverter(typeof(SortOptionsConverter))]
public sealed class SortOptions
{
	public enum SortKind { Field, Score, Doc, GeoDistance, Script }

	/// <summary>The kind of sort variant.</summary>
	public SortKind VariantKind { get; }

	/// <summary>The variant value.</summary>
	public object Variant { get; }

	private SortOptions(SortKind kind, object value)
	{
		VariantKind = kind;
		Variant = value;
	}

	/// <summary>Creates a field sort variant.</summary>
	public static SortOptions Field(FieldSort value) => new(SortKind.Field, value);

	/// <summary>Creates a score sort variant.</summary>
	public static SortOptions Score(ScoreSort? value = null) => new(SortKind.Score, value ?? new ScoreSort());

	/// <summary>Creates a doc sort variant.</summary>
	public static SortOptions Doc(ScoreSort? value = null) => new(SortKind.Doc, value ?? new ScoreSort());

	/// <summary>Creates a geo distance sort variant.</summary>
	public static SortOptions GeoDistance(GeoDistanceSort value) => new(SortKind.GeoDistance, value);

	/// <summary>Creates a script sort variant.</summary>
	public static SortOptions Script(ScriptSort value) => new(SortKind.Script, value);

	/// <summary>Creates a field sort ascending on the given field.</summary>
	public static SortOptions Ascending(string field) =>
		Field(new FieldSort { FieldName = field, Order = SortOrder.Asc });

	/// <summary>Creates a field sort descending on the given field.</summary>
	public static SortOptions Descending(string field) =>
		Field(new FieldSort { FieldName = field, Order = SortOrder.Desc });

	/// <summary>Implicit conversion from string to a field sort (ascending by default).</summary>
	public static implicit operator SortOptions(string field) =>
		Field(new FieldSort { FieldName = field });
}
