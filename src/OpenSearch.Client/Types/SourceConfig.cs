using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Controls which source fields are returned. Can be a boolean (true/false to include/exclude entire source)
/// or a <see cref="SourceFilter"/> for selective inclusion/exclusion.
/// </summary>
[JsonConverter(typeof(SourceConfigConverter))]
public sealed class SourceConfig
{
	private readonly object _value;

	private SourceConfig(object value) => _value = value;

	/// <summary>Creates a SourceConfig that enables or disables source fetching.</summary>
	public static SourceConfig Enabled(bool fetch) => new(fetch);

	/// <summary>Creates a SourceConfig with a source filter.</summary>
	public static SourceConfig Filter(SourceFilter filter) => new(filter);

	/// <summary>Implicit conversion from bool.</summary>
	public static implicit operator SourceConfig(bool fetch) => Enabled(fetch);

	/// <summary>Whether the value is a boolean.</summary>
	public bool IsBool => _value is bool;

	/// <summary>Whether the value is a SourceFilter.</summary>
	public bool IsFilter => _value is SourceFilter;

	/// <summary>Gets the boolean value.</summary>
	public bool AsBool() => (bool)_value;

	/// <summary>Gets the SourceFilter value.</summary>
	public SourceFilter AsFilter() => (SourceFilter)_value;

	internal object RawValue => _value;
}

/// <summary>
/// Filters which source fields to include or exclude.
/// </summary>
public sealed class SourceFilter
{
	/// <summary>Fields to include in the response.</summary>
	[JsonPropertyName("includes")]
	public List<string>? Includes { get; set; }

	/// <summary>Fields to exclude from the response.</summary>
	[JsonPropertyName("excludes")]
	public List<string>? Excludes { get; set; }
}
