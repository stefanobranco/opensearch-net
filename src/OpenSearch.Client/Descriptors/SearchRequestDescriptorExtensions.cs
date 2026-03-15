using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Extension methods for the generated <see cref="SearchRequestDescriptor"/> to add
/// fluent aggregation dictionary and highlight field building.
/// </summary>
public static class SearchRequestDescriptorExtensions
{
	public static SearchRequestDescriptor Aggregations(
		this SearchRequestDescriptor d, Action<AggregationsDictDescriptor> configure)
	{
		var desc = new AggregationsDictDescriptor();
		configure(desc);
		d._value.Aggregations = desc;
		return d;
	}
}
