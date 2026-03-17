using System.Text.Json;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience extension methods for <see cref="ScriptQueryDescriptor"/> to provide
/// typed inline script configuration instead of raw <c>JsonElement</c>.
/// </summary>
public static class ScriptQueryExtensions
{
	/// <summary>
	/// Sets the script as an inline script with source, optional language, and optional parameters.
	/// </summary>
	public static ScriptQueryDescriptor Script(this ScriptQueryDescriptor d,
		string source, string? lang = null, Dictionary<string, object>? @params = null)
	{
		var script = new Dictionary<string, object> { ["source"] = source };
		if (lang is not null) script["lang"] = lang;
		if (@params is not null) script["params"] = @params;

		d._value.Script = JsonSerializer.SerializeToElement(script);
		return d;
	}
}
