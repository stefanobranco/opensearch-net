namespace OpenSearch.Client;

/// <summary>
/// Named ordering shortcuts for terms-aggregation descriptors. The underlying <c>Order</c> property is an
/// <see cref="AggregateOrder"/>, and <c>Include</c> accepts a string/array/<see cref="TermsInclude"/>
/// directly, so no JsonElement wrapping is needed.
/// </summary>
public static class AggregationDescriptorExtensions
{
	/// <summary>Order by document count descending (default).</summary>
	public static TermsAggregationFieldsDescriptor CountDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = AggregateOrder.CountDescending; return d; }

	/// <summary>Order by document count ascending.</summary>
	public static TermsAggregationFieldsDescriptor CountAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = AggregateOrder.CountAscending; return d; }

	/// <summary>Order by bucket key ascending.</summary>
	public static TermsAggregationFieldsDescriptor KeyAscending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = AggregateOrder.KeyAscending; return d; }

	/// <summary>Order by bucket key descending.</summary>
	public static TermsAggregationFieldsDescriptor KeyDescending(this TermsAggregationFieldsDescriptor d)
	{ d._value.Order = AggregateOrder.KeyDescending; return d; }

	/// <summary>Order by a sub-aggregation metric.</summary>
	public static TermsAggregationFieldsDescriptor OrderBy(this TermsAggregationFieldsDescriptor d,
		string subAggName, SortOrder order = SortOrder.Asc)
	{ d._value.Order = AggregateOrder.By(subAggName, order); return d; }
}
