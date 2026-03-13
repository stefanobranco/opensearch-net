using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Converts enum values to their wire-format strings for use in query parameters.
/// Uses <see cref="EnumMemberAttribute"/> values when present, falling back to the member name.
/// </summary>
internal static class QueryParamSerializer
{
	private static readonly ConcurrentDictionary<Type, Dictionary<object, string>> s_cache = new();

	public static string Serialize<T>(T value) where T : struct, Enum
	{
		var lookup = s_cache.GetOrAdd(typeof(T), static t =>
		{
			var dict = new Dictionary<object, string>();
			foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var attr = field.GetCustomAttribute<EnumMemberAttribute>();
				dict[field.GetValue(null)!] = attr?.Value ?? field.Name;
			}
			return dict;
		});

		return lookup.TryGetValue(value, out var wireValue) ? wireValue : value.ToString();
	}
}
