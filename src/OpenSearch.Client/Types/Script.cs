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
	public static Script Inline(string source, string? lang = null, Dictionary<string, JsonElement>? @params = null) =>
		new() { Source = source, Lang = lang, Params = @params };

	/// <summary>Creates a stored script reference.</summary>
	public static Script Stored(string id, Dictionary<string, JsonElement>? @params = null) =>
		new() { Id = id, Params = @params };
}
