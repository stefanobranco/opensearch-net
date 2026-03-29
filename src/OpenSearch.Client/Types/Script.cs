using System.Text.Json;

namespace OpenSearch.Client;

/// <summary>
/// Script for use in queries, aggregations, and updates.
/// Covers both inline scripts (<see cref="Source"/>) and stored scripts (<see cref="Id"/>).
/// </summary>
public sealed class Script
{
	/// <summary>The script source code (inline script).</summary>
	public string? Source { get; set; }

	/// <summary>The script language (default: "painless").</summary>
	public string? Lang { get; set; }

	/// <summary>Named parameters passed to the script.</summary>
	public Dictionary<string, JsonElement>? Params { get; set; }

	/// <summary>The ID of a stored script.</summary>
	public string? Id { get; set; }

	/// <summary>Creates an inline script.</summary>
	public static Script Inline(string source, string? lang = null, Dictionary<string, object>? @params = null) =>
		new() { Source = source, Lang = lang, Params = ToJsonElements(@params) };

	/// <summary>Creates an inline script with pre-serialized parameters.</summary>
	public static Script Inline(string source, string? lang, Dictionary<string, JsonElement> @params) =>
		new() { Source = source, Lang = lang, Params = @params };

	/// <summary>Creates a stored script reference.</summary>
	public static Script Stored(string id, Dictionary<string, object>? @params = null) =>
		new() { Id = id, Params = ToJsonElements(@params) };

	private static Dictionary<string, JsonElement>? ToJsonElements(Dictionary<string, object>? source)
	{
		if (source is null || source.Count == 0) return null;
		var result = new Dictionary<string, JsonElement>(source.Count, StringComparer.Ordinal);
		foreach (var (key, value) in source)
			result[key] = JsonSerializer.SerializeToElement(value);
		return result;
	}
}
