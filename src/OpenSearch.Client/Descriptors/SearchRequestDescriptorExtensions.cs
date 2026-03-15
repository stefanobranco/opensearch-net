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

	/// <summary>Shorthand for <c>Source(SourceConfig.Enabled(fetch))</c>.</summary>
	public static SearchRequestDescriptor Source(
		this SearchRequestDescriptor d, bool fetch)
	{
		d._value.Source = SourceConfig.Enabled(fetch);
		return d;
	}

	/// <summary>Shorthand for <c>Source(SourceConfig.Enabled(fetch))</c>.</summary>
	public static InnerHitsDescriptor Source(
		this InnerHitsDescriptor d, bool fetch)
	{
		d._value.Source = SourceConfig.Enabled(fetch);
		return d;
	}
}
