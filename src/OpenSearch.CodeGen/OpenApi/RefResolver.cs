using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Resolves $ref pointers across multiple YAML files with caching.
/// </summary>
public sealed class RefResolver
{
	private readonly Dictionary<string, YamlMappingNode> _fileCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, YamlNode> _refCache = new(StringComparer.Ordinal);
	private readonly string _specDir;

	public RefResolver(string specDir) => _specDir = specDir;

	/// <summary>
	/// Loads and caches a YAML file, returning its root mapping node.
	/// </summary>
	public YamlMappingNode LoadFile(string relativePath)
	{
		var fullPath = Path.GetFullPath(Path.Combine(_specDir, relativePath));
		if (_fileCache.TryGetValue(fullPath, out var cached))
			return cached;

		using var reader = new StreamReader(fullPath);
		var yaml = new YamlStream();
		yaml.Load(reader);
		var root = (YamlMappingNode)yaml.Documents[0].RootNode;
		_fileCache[fullPath] = root;
		return root;
	}

	/// <summary>
	/// Resolves a $ref string relative to a context file.
	/// Format: "relative/path.yaml#/json/pointer" or "#/json/pointer" (same file).
	/// </summary>
	public YamlNode Resolve(string refString, string contextFile)
	{
		var cacheKey = $"{contextFile}|{refString}";
		if (_refCache.TryGetValue(cacheKey, out var cached))
			return cached;

		var hashIndex = refString.IndexOf('#');
		string filePath;
		string jsonPointer;

		if (hashIndex < 0)
		{
			filePath = refString;
			jsonPointer = "";
		}
		else if (hashIndex == 0)
		{
			filePath = contextFile;
			jsonPointer = refString[1..];
		}
		else
		{
			filePath = refString[..hashIndex];
			jsonPointer = refString[(hashIndex + 1)..];
		}

		// Resolve relative path against context file's directory
		if (filePath != contextFile)
		{
			var contextDir = Path.GetDirectoryName(contextFile) ?? _specDir;
			filePath = Path.GetFullPath(Path.Combine(contextDir, filePath));
		}

		// Normalize to relative path from specDir for loading
		var normalizedPath = Path.GetRelativePath(_specDir, filePath);
		var root = LoadFile(normalizedPath);

		// Walk JSON pointer
		YamlNode current = root;
		if (!string.IsNullOrEmpty(jsonPointer))
		{
			var parts = jsonPointer.TrimStart('/').Split('/');
			foreach (var part in parts)
			{
				if (current is YamlMappingNode mapping)
				{
					current = mapping.Children[new YamlScalarNode(part)];
				}
				else
				{
					throw new InvalidOperationException(
						$"Cannot navigate pointer '{jsonPointer}' in '{refString}' from '{contextFile}': " +
						$"expected mapping node but got {current.GetType().Name} at part '{part}'");
				}
			}
		}

		_refCache[cacheKey] = current;
		return current;
	}

	/// <summary>
	/// Resolves a $ref and returns it as a mapping node.
	/// </summary>
	public YamlMappingNode ResolveMapping(string refString, string contextFile) =>
		(YamlMappingNode)Resolve(refString, contextFile);

	/// <summary>
	/// Given a $ref string and the file it appeared in, returns the updated context file
	/// that should be used for further resolution of refs within the resolved node.
	/// </summary>
	public string ResolveContextFile(string refString, string contextFile)
	{
		var hashIndex = refString.IndexOf('#');
		if (hashIndex <= 0)
			return contextFile;

		var relPath = refString[..hashIndex];
		var contextDir = Path.GetDirectoryName(contextFile) ?? _specDir;
		return Path.GetFullPath(Path.Combine(contextDir, relPath));
	}
}
