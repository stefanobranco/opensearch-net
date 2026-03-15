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
		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[name] = JsonSerializer.SerializeToElement(
			fieldSuggester, SuggesterSerializerOptions.Instance);
		return d;
	}
}

/// <summary>
/// Cached <see cref="JsonSerializerOptions"/> with snake_case naming for suggest serialization.
/// </summary>
internal static class SuggesterSerializerOptions
{
	internal static readonly JsonSerializerOptions Instance = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};
}
