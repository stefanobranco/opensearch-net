using System.Text.Json;

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
}
