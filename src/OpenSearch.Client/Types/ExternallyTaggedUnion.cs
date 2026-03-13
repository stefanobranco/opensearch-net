namespace OpenSearch.Client;

/// <summary>
/// Utility methods for OpenSearch's "typed_keys" format, where aggregation and suggester
/// results use keys of the form <c>"type#name"</c> to encode both the variant type and
/// the user-assigned name in a single JSON property name.
/// </summary>
public static class ExternallyTaggedUnion
{
	/// <summary>
	/// Parses a <c>"type#name"</c> key into its type and optional name components.
	/// If the key does not contain a <c>#</c>, the entire key is treated as the type
	/// and <paramref name="Name"/> will be <c>null</c>.
	/// </summary>
	/// <param name="key">The typed_keys key to parse.</param>
	/// <returns>A tuple of (Type, Name) where Name may be null.</returns>
	public static (string Type, string? Name) ParseKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		var hashIndex = key.IndexOf('#');
		if (hashIndex < 0) return (key, null);
		return (key[..hashIndex], key[(hashIndex + 1)..]);
	}

	/// <summary>
	/// Builds a <c>"type#name"</c> key from the given type and optional name.
	/// If <paramref name="name"/> is <c>null</c>, returns just the type.
	/// </summary>
	public static string BuildKey(string type, string? name) =>
		name is null ? type : $"{type}#{name}";
}
