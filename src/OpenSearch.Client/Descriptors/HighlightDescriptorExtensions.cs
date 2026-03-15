namespace OpenSearch.Client.Core;

/// <summary>
/// Extension methods for the generated <see cref="HighlightDescriptor"/> to add
/// a fluent field entry builder.
/// </summary>
public static class HighlightDescriptorExtensions
{
	/// <summary>
	/// Builds the highlight fields dictionary using per-field entry descriptors.
	/// Each entry must call <c>.Field(name)</c> to set the dictionary key.
	/// </summary>
	public static HighlightDescriptor Fields(
		this HighlightDescriptor d, params Action<HighlightFieldEntryDescriptor>[] configure)
	{
		var dict = new Dictionary<string, HighlightField>();
		foreach (var action in configure)
		{
			var entry = new HighlightFieldEntryDescriptor();
			action(entry);
			dict[entry._fieldName ?? throw new InvalidOperationException(
				"HighlightFieldEntryDescriptor requires .Field(name) to be called.")] = entry;
		}
		d._value.Fields = dict;
		return d;
	}
}
