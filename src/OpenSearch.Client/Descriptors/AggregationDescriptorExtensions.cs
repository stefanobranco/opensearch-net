using System.Text.Json;
using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience extension methods for generated aggregation descriptors that accept
/// <see cref="JsonElement"/> where callers typically pass typed values.
/// </summary>
public static class AggregationDescriptorExtensions
{
	private static readonly JsonElement s_countDesc = JsonSerializer.SerializeToElement(new { _count = "desc" });
	private static readonly JsonElement s_countAsc = JsonSerializer.SerializeToElement(new { _count = "asc" });
	private static readonly JsonElement s_keyAsc = JsonSerializer.SerializeToElement(new { _key = "asc" });
	private static readonly JsonElement s_keyDesc = JsonSerializer.SerializeToElement(new { _key = "desc" });

	// ── TermsAggregationFieldsDescriptor: Order ──

	/// <summary>Order by document count descending (default).</summary>
	public static TermsAggregationFieldsDescriptor CountDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = s_countDesc; return d; }

	/// <summary>Order by document count ascending.</summary>
	public static TermsAggregationFieldsDescriptor CountAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = s_countAsc; return d; }

	/// <summary>Order by bucket key ascending.</summary>
	public static TermsAggregationFieldsDescriptor KeyAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = s_keyAsc; return d; }

	/// <summary>Order by bucket key descending.</summary>
	public static TermsAggregationFieldsDescriptor KeyDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = s_keyDesc; return d; }

	/// <summary>Order by a sub-aggregation metric.</summary>
	public static TermsAggregationFieldsDescriptor OrderBy(this TermsAggregationFieldsDescriptor d,
		string subAggName, SortOrder order = SortOrder.Asc)
	{
		var dict = new Dictionary<string, string> { [subAggName] = order == SortOrder.Asc ? "asc" : "desc" };
		d._value.Order = JsonSerializer.SerializeToElement(dict);
		return d;
	}

	// ── TermsAggregationFieldsDescriptor: Include ──

	/// <summary>Include only buckets matching the specified terms.</summary>
	public static TermsAggregationFieldsDescriptor Include(this TermsAggregationFieldsDescriptor d, List<string> values)
	{ d._value.Include = JsonSerializer.SerializeToElement(values); return d; }

	/// <summary>Include only buckets matching the specified regex pattern.</summary>
	public static TermsAggregationFieldsDescriptor Include(this TermsAggregationFieldsDescriptor d, string pattern)
	{ d._value.Include = JsonSerializer.SerializeToElement(pattern); return d; }
}
