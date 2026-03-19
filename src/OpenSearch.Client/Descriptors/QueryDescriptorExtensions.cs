using System.Linq.Expressions;
using System.Text.Json;
using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience extension methods for generated query descriptors that accept
/// <see cref="JsonElement"/> where callers typically pass strings or primitives.
/// </summary>
public static class QueryDescriptorExtensions
{
	// ── TermQueryDescriptor ──

	public static TermQueryDescriptor Value(this TermQueryDescriptor d, string value)
	{ d._value.Value = JsonSerializer.SerializeToElement(value); return d; }

	public static TermQueryDescriptor Value(this TermQueryDescriptor d, bool value)
	{ d._value.Value = JsonSerializer.SerializeToElement(value); return d; }

	public static TermQueryDescriptor Value(this TermQueryDescriptor d, long value)
	{ d._value.Value = JsonSerializer.SerializeToElement(value); return d; }

	public static TermQueryDescriptor Value(this TermQueryDescriptor d, double value)
	{ d._value.Value = JsonSerializer.SerializeToElement(value); return d; }

	public static TermQueryDescriptor Value(this TermQueryDescriptor d, object value)
	{ d._value.Value = JsonSerializer.SerializeToElement(value); return d; }

	// ── MatchQueryDescriptor ──

	public static MatchQueryDescriptor Query(this MatchQueryDescriptor d, string value)
	{ d._value.Query = JsonSerializer.SerializeToElement(value); return d; }

	// Note: MatchPhraseQueryDescriptor.Query() and MatchPhrasePrefixQueryDescriptor.Query()
	// already accept string? natively — no extension needed.

	// ── BoolQueryDescriptor / BoolQueryDescriptor<T> ──

	/// <summary>Accepts an int for the common case (e.g., 1). The spec allows both
	/// integers and percentage strings like "75%", hence the underlying string type.</summary>
	public static BoolQueryDescriptor MinimumShouldMatch(this BoolQueryDescriptor d, int value)
	{ d._value.MinimumShouldMatch = value.ToString(); return d; }

	/// <summary>Accepts an int for the common case (e.g., 1). The spec allows both
	/// integers and percentage strings like "75%", hence the underlying string type.</summary>
	public static BoolQueryDescriptor<TDocument> MinimumShouldMatch<TDocument>(
		this BoolQueryDescriptor<TDocument> d, int value)
	{ d._value.MinimumShouldMatch = value.ToString(); return d; }

	// ── TermsQueryDescriptor ──

	/// <summary>
	/// Sets the field and terms values for a terms query.
	/// Equivalent to: <c>{ "field_name": ["val1", "val2"] }</c>
	/// </summary>
	public static TermsQueryDescriptor Field(this TermsQueryDescriptor d, string field, params string[] values)
	{
		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[field] = JsonSerializer.SerializeToElement(values);
		return d;
	}

	/// <summary>
	/// Sets the field and terms values for a terms query with typed values.
	/// </summary>
	public static TermsQueryDescriptor Field<TValue>(this TermsQueryDescriptor d, string field, params TValue[] values)
	{
		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[field] = JsonSerializer.SerializeToElement(values);
		return d;
	}

	/// <summary>
	/// Sets the field (via expression) and terms values for a terms query.
	/// </summary>
	public static TermsQueryDescriptor Field<TDocument>(this TermsQueryDescriptor d,
		Expression<Func<TDocument, object>> field, params string[] values)
	{
		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[FieldExpressionVisitor.Resolve(field)] = JsonSerializer.SerializeToElement(values);
		return d;
	}

	/// <summary>
	/// Sets the field (via expression) and terms values for a terms query with typed values.
	/// </summary>
	public static TermsQueryDescriptor Field<TDocument, TValue>(this TermsQueryDescriptor d,
		Expression<Func<TDocument, object>> field, params TValue[] values)
	{
		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[FieldExpressionVisitor.Resolve(field)] = JsonSerializer.SerializeToElement(values);
		return d;
	}

	// ── MoreLikeThisQueryDescriptor ──

	/// <summary>
	/// Adds a "like this document" item by index and ID, avoiding manual JsonElement construction.
	/// Usage: <c>.Like("my-index", "doc-id")</c>
	/// </summary>
	public static MoreLikeThisQueryDescriptor LikeDocument(this MoreLikeThisQueryDescriptor d,
		string index, string id)
	{
		d._value.Like ??= [];
		d._value.Like.Add(JsonSerializer.SerializeToElement(new { _index = index, _id = id }));
		return d;
	}

	/// <summary>
	/// Adds a "like this text" item.
	/// </summary>
	public static MoreLikeThisQueryDescriptor LikeText(this MoreLikeThisQueryDescriptor d, string text)
	{
		d._value.Like ??= [];
		d._value.Like.Add(JsonSerializer.SerializeToElement(text));
		return d;
	}

	// ── CompletionSuggesterDescriptor ──

	/// <summary>
	/// Sets completion suggestion contexts with string values, avoiding manual JsonElement construction.
	/// Usage: <c>.Contexts("language", "en")</c>
	/// </summary>
	public static CompletionSuggesterDescriptor Context(this CompletionSuggesterDescriptor d,
		string name, params string[] values)
	{
		d._value.Contexts ??= new();
		d._value.Contexts[name] = values
			.Select(v => JsonSerializer.SerializeToElement(new { context = v }))
			.ToList();
		return d;
	}
}
