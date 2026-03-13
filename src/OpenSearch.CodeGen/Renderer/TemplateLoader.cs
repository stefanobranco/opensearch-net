using Scriban;
using Scriban.Runtime;

namespace OpenSearch.CodeGen.Renderer;

/// <summary>
/// Loads and caches Scriban templates from the Templates directory.
/// </summary>
public sealed class TemplateLoader
{
	private readonly string _templatesDir;
	private readonly Dictionary<string, Template> _cache = new(StringComparer.Ordinal);

	public TemplateLoader(string templatesDir)
	{
		_templatesDir = templatesDir;
	}

	public Template Load(string name)
	{
		if (_cache.TryGetValue(name, out var cached))
			return cached;

		var path = Path.Combine(_templatesDir, name);
		var text = File.ReadAllText(path);
		var template = Template.Parse(text, path);

		if (template.HasErrors)
		{
			var errors = string.Join("\n", template.Messages.Select(m => m.ToString()));
			throw new InvalidOperationException($"Template '{name}' has errors:\n{errors}");
		}

		_cache[name] = template;
		return template;
	}
}
