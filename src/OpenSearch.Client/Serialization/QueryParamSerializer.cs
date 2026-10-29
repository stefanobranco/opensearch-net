using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Converts path and query parameter values to their wire-format strings. Enums use
/// <see cref="EnumMemberAttribute"/> values when present, falling back to the member name;
/// numbers format with the invariant culture.
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

	public static string Serialize<T>(IEnumerable<T> values) where T : struct, Enum =>
		string.Join(",", values.Select(Serialize));

	public static string Serialize(int value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Serialize(long value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Serialize(float value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);
}
