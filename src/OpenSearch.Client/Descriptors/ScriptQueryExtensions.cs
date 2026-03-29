using System.Text.Json;

namespace OpenSearch.Client.Common;

/// <summary>
/// Convenience extension methods for <see cref="ScriptQueryDescriptor"/> to provide
/// typed inline script configuration.
/// </summary>
public static class ScriptQueryExtensions
{
	/// <summary>
	/// Sets the script as an inline script with source, optional language, and optional parameters.
	/// </summary>
	public static ScriptQueryDescriptor Script(this ScriptQueryDescriptor d,
		string source, string? lang = null, Dictionary<string, JsonElement>? @params = null)
	{
		d._value.Script = OpenSearch.Client.Script.Inline(source, lang, @params);
		return d;
	}
}
