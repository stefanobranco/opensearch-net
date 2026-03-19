using System.Text.Json;

namespace OpenSearch.Client.Indices;

/// <summary>
/// Convenience extension methods for <see cref="AnalyzeIndexRequestDescriptor"/> to provide
/// typed text input instead of raw <c>JsonElement</c>.
/// </summary>
public static class AnalyzeRequestExtensions
{
	/// <summary>Sets the text to analyze as a single string.</summary>
	public static AnalyzeIndexRequestDescriptor Text(this AnalyzeIndexRequestDescriptor d, string text)
	{
		d._value.Text = JsonSerializer.SerializeToElement(text);
		return d;
	}

	/// <summary>Sets the text to analyze as multiple strings.</summary>
	public static AnalyzeIndexRequestDescriptor Text(this AnalyzeIndexRequestDescriptor d, params string[] texts)
	{
		d._value.Text = JsonSerializer.SerializeToElement(texts);
		return d;
	}
}
