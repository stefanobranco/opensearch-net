using System.Text.Json;
using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience extension methods for generated aggregation descriptors that accept
/// <see cref="JsonElement"/> where callers typically pass typed values.
/// </summary>
public static class AggregationDescriptorExtensions
{
	// ── TermsAggregationFieldsDescriptor: Order ──

	/// <summary>Order by document count descending (default).</summary>
	public static TermsAggregationFieldsDescriptor CountDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = JsonSerializer.SerializeToElement(new { _count = "desc" }); return d; }

	/// <summary>Order by document count ascending.</summary>
	public static TermsAggregationFieldsDescriptor CountAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = JsonSerializer.SerializeToElement(new { _count = "asc" }); return d; }

	/// <summary>Order by bucket key ascending.</summary>
	public static TermsAggregationFieldsDescriptor KeyAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = JsonSerializer.SerializeToElement(new { _key = "asc" }); return d; }

	/// <summary>Order by bucket key descending.</summary>
	public static TermsAggregationFieldsDescriptor KeyDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = JsonSerializer.SerializeToElement(new { _key = "desc" }); return d; }

	/// <summary>Order by a sub-aggregation metric.</summary>
	public static TermsAggregationFieldsDescriptor OrderBy(this TermsAggregationFieldsDescriptor d,
		string subAggName, bool ascending = true)
	{
		var order = new Dictionary<string, string> { [subAggName] = ascending ? "asc" : "desc" };
		d._value.Order = JsonSerializer.SerializeToElement(order);
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
