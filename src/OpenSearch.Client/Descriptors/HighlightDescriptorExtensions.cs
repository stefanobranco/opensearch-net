using OpenSearch.Client.Common;

namespace OpenSearch.Client.Core;

/// <summary>
/// Extension methods for the generated <see cref="HighlightDescriptor"/> and
/// <see cref="HighlightFieldDescriptor"/> to bridge generic and non-generic descriptors.
/// </summary>
public static class HighlightDescriptorExtensions
{
	/// <summary>
	/// Builds the highlight fields dictionary using the generated <see cref="HighlightFieldDescriptor"/>.
	/// Each tuple pairs a field name with a configuration action.
	/// </summary>
	public static HighlightDescriptor Fields(
		this HighlightDescriptor d,
		params (string Name, Action<HighlightFieldDescriptor> Configure)[] fields)
	{
		var dict = new Dictionary<string, HighlightField>(fields.Length);
		foreach (var (name, configure) in fields)
		{
			var desc = new HighlightFieldDescriptor();
			configure(desc);
			dict[name] = desc;
		}
		d._value.Fields = dict;
		return d;
	}

	/// <summary>
	/// Sets the highlight query using a generic <see cref="QueryContainerDescriptor{TDocument}"/>,
	/// allowing callers to pass <c>Action&lt;QueryContainerDescriptor&lt;T&gt;&gt;</c> lambdas directly.
	/// </summary>
	public static HighlightFieldDescriptor HighlightQuery<TDocument>(
		this HighlightFieldDescriptor d,
		Action<QueryContainerDescriptor<TDocument>> configure)
	{
		var descriptor = new QueryContainerDescriptor<TDocument>();
		configure(descriptor);
		d._value.HighlightQuery = descriptor;
		return d;
	}
}
