using System.Text.Json;
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

	/// <summary>
	/// Adds named field suggesters to the suggest section, serializing them with
	/// snake_case property naming. Without this, <c>JsonSerializer.SerializeToElement</c>
	/// uses PascalCase and OpenSearch silently ignores the unknown property names.
	/// </summary>
	public static SuggesterDescriptor Add(this SuggesterDescriptor d,
		string name, FieldSuggester fieldSuggester)
	{
		d._value.ExtensionData ??= new();
		d._value.ExtensionData[name] = JsonSerializer.SerializeToElement(
			fieldSuggester, OpenSearchJsonOptions.RequestSerialization);
		return d;
	}

	// ── Named suggester convenience methods ──

	/// <summary>Adds a named completion suggester.</summary>
	public static SuggesterDescriptor Completion(this SuggesterDescriptor d,
		string name, Action<CompletionSuggesterDescriptor> configure, string? prefix = null, string? text = null)
	{
		var desc = new CompletionSuggesterDescriptor();
		configure(desc);
		return d.Add(name, new FieldSuggester { Completion = desc, Prefix = prefix, Text = text });
	}

	/// <summary>Adds a named term suggester.</summary>
	public static SuggesterDescriptor Term(this SuggesterDescriptor d,
		string name, Action<TermSuggesterDescriptor> configure, string? text = null)
	{
		var desc = new TermSuggesterDescriptor();
		configure(desc);
		return d.Add(name, new FieldSuggester { Term = desc, Text = text });
	}

	/// <summary>Adds a named phrase suggester.</summary>
	public static SuggesterDescriptor Phrase(this SuggesterDescriptor d,
		string name, Action<PhraseSuggesterDescriptor> configure, string? text = null)
	{
		var desc = new PhraseSuggesterDescriptor();
		configure(desc);
		return d.Add(name, new FieldSuggester { Phrase = desc, Text = text });
	}
}

