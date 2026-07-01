namespace OpenSearch.Client;

/// <summary>
/// Convenience extension methods for <see cref="AnalyzeIndexRequestDescriptor"/> to provide
/// single-string text input alongside the generated list form.
/// </summary>
public static class AnalyzeRequestExtensions
{
	/// <summary>Sets the text to analyze as a single string.</summary>
	public static AnalyzeIndexRequestDescriptor Text(this AnalyzeIndexRequestDescriptor d, string text)
	{
		d._value.Text = [text];
		return d;
	}

	/// <summary>Sets the text to analyze as multiple strings.</summary>
	public static AnalyzeIndexRequestDescriptor Text(this AnalyzeIndexRequestDescriptor d, params string[] texts)
	{
		d._value.Text = [.. texts];
		return d;
	}
}
