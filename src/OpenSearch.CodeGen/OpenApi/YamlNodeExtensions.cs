using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.OpenApi;

/// <summary>
/// Shared helpers for reading values from YAML nodes.
/// </summary>
internal static class YamlNodeExtensions
{
	/// <summary>
	/// Reads a scalar string value from a mapping node by key.
	/// </summary>
	public static string? GetScalar(this YamlMappingNode node, string key)
	{
		if (node.Children.TryGetValue(new YamlScalarNode(key), out var child) && child is YamlScalarNode scalar)
			return scalar.Value;
		return null;
	}
}
