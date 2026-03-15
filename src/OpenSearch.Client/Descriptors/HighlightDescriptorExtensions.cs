namespace OpenSearch.Client.Core;

/// <summary>
/// Extension methods for the generated <see cref="HighlightDescriptor"/> to add
/// a fluent field entry builder using tuples of (fieldName, configure).
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
}
