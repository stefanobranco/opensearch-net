using System.Text.Json;

namespace OpenSearch.Client;

/// <summary>
/// Fluent builder for range query values. Builds a <see cref="JsonElement"/>
/// suitable for range queries in <c>QueryContainer</c>.
/// </summary>
public sealed class RangeQueryDescriptor
{
	private readonly Dictionary<string, object> _props = new();

	public RangeQueryDescriptor Gte(object value) { _props["gte"] = value; return this; }
	public RangeQueryDescriptor Gt(object value) { _props["gt"] = value; return this; }
	public RangeQueryDescriptor Lte(object value) { _props["lte"] = value; return this; }
	public RangeQueryDescriptor Lt(object value) { _props["lt"] = value; return this; }
	public RangeQueryDescriptor Format(string value) { _props["format"] = value; return this; }
	public RangeQueryDescriptor TimeZone(string value) { _props["time_zone"] = value; return this; }
	public RangeQueryDescriptor Boost(float value) { _props["boost"] = value; return this; }
	public RangeQueryDescriptor Relation(string value) { _props["relation"] = value; return this; }

	public static implicit operator JsonElement(RangeQueryDescriptor d) =>
		JsonSerializer.SerializeToElement(d._props);
}
