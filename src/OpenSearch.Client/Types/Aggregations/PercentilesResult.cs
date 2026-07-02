using System.Globalization;
using System.Text.Json;

namespace OpenSearch.Client;

/// <summary>
/// The result of a <c>percentiles</c>, <c>percentile_ranks</c>, or <c>percentiles_bucket</c>
/// aggregation. OpenSearch returns either a keyed map (<c>{"50.0": 3.0, ...}</c>, the default) or,
/// with <c>keyed=false</c>, an array of <c>{key, value}</c> items — both parse into the same shape.
/// </summary>
public sealed class PercentilesResult
{
	private readonly Dictionary<double, double?> _values;
	private readonly Dictionary<double, string> _valuesAsString;

	private PercentilesResult(Dictionary<double, double?> values, Dictionary<double, string> valuesAsString)
	{
		_values = values;
		_valuesAsString = valuesAsString;
	}

	/// <summary>The percentile→value map (e.g. <c>[50.0] = 3.0</c>). Values are null when there was no data.</summary>
	public IReadOnlyDictionary<double, double?> Values => _values;

	/// <summary>The value at the given percentile, or <c>null</c> when absent.</summary>
	public double? Percentile(double percentile) =>
		_values.TryGetValue(percentile, out var v) ? v : null;

	/// <summary>The formatted string value at the given percentile (present when a format was requested).</summary>
	public string? PercentileAsString(double percentile) =>
		_valuesAsString.TryGetValue(percentile, out var v) ? v : null;

	internal static PercentilesResult? Parse(JsonElement? valuesElement)
	{
		if (valuesElement is not { } el || el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
			return null;

		var values = new Dictionary<double, double?>();
		var asString = new Dictionary<double, string>();

		if (el.ValueKind == JsonValueKind.Object)
		{
			// Keyed form: {"50.0": 3.0, "50.0_as_string": "3ms", ...}
			foreach (var prop in el.EnumerateObject())
			{
				var key = prop.Name;
				var isAsString = key.EndsWith("_as_string", StringComparison.Ordinal);
				if (isAsString)
					key = key[..^"_as_string".Length];
				if (!double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentile))
					continue;

				if (isAsString)
					asString[percentile] = prop.Value.GetString() ?? "";
				else
					values[percentile] = ReadValue(prop.Value);
			}
		}
		else if (el.ValueKind == JsonValueKind.Array)
		{
			// Array form (keyed=false): [{"key": "50.0", "value": 3.0, "value_as_string": "3ms"}, ...]
			foreach (var item in el.EnumerateArray())
			{
				if (!item.TryGetProperty("key", out var keyEl))
					continue;
				var key = keyEl.ValueKind == JsonValueKind.String ? keyEl.GetString()! : keyEl.GetRawText();
				if (!double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentile))
					continue;

				values[percentile] = item.TryGetProperty("value", out var valueEl) ? ReadValue(valueEl) : null;
				if (item.TryGetProperty("value_as_string", out var strEl) && strEl.ValueKind == JsonValueKind.String)
					asString[percentile] = strEl.GetString()!;
			}
		}
		else
		{
			return null;
		}

		return new PercentilesResult(values, asString);
	}

	private static double? ReadValue(JsonElement el) =>
		el.ValueKind switch
		{
			JsonValueKind.Number => el.GetDouble(),
			JsonValueKind.String when double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
			_ => null,
		};
}
