using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Generic companion to <see cref="BoolQueryDescriptor"/> that propagates
/// <typeparamref name="TDocument"/> to Must/MustNot/Should/Filter clauses.
/// </summary>
public sealed class BoolQueryDescriptor<TDocument>
{
	internal BoolQuery _value = new();

	public BoolQueryDescriptor<TDocument> Boost(float? value) { _value.Boost = value; return this; }
	public BoolQueryDescriptor<TDocument> Name(string? value) { _value.Name = value; return this; }
	public BoolQueryDescriptor<TDocument> MinimumShouldMatch(string? value) { _value.MinimumShouldMatch = value; return this; }
	public BoolQueryDescriptor<TDocument> AdjustPureNegative(bool? value) { _value.AdjustPureNegative = value; return this; }

	public BoolQueryDescriptor<TDocument> Must(
		params Action<QueryContainerDescriptor<TDocument>>[] configure)
	{ _value.Must = BuildQueryList(configure); return this; }

	public BoolQueryDescriptor<TDocument> MustNot(
		params Action<QueryContainerDescriptor<TDocument>>[] configure)
	{ _value.MustNot = BuildQueryList(configure); return this; }

	public BoolQueryDescriptor<TDocument> Should(
		params Action<QueryContainerDescriptor<TDocument>>[] configure)
	{ _value.Should = BuildQueryList(configure); return this; }

	public BoolQueryDescriptor<TDocument> Filter(
		params Action<QueryContainerDescriptor<TDocument>>[] configure)
	{ _value.Filter = BuildQueryList(configure); return this; }

	private static List<QueryContainer> BuildQueryList(
		Action<QueryContainerDescriptor<TDocument>>[] configure)
	{
		var list = new List<QueryContainer>(configure.Length);
		foreach (var action in configure)
		{
			var descriptor = new QueryContainerDescriptor<TDocument>();
			action(descriptor);
			list.Add(((QueryContainer)descriptor)!);
		}
		return list;
	}

	public static implicit operator BoolQuery(BoolQueryDescriptor<TDocument> d) => d._value;
}
