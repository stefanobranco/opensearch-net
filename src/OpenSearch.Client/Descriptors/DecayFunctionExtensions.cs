using System.Text.Json;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience extension methods for <see cref="DecayFunctionDescriptor"/> to provide
/// typed field-level decay configuration instead of raw <c>AdditionalProperties</c>.
/// </summary>
public static class DecayFunctionExtensions
{
	/// <summary>
	/// Configures a decay function for a specific field with typed parameters.
	/// </summary>
	/// <param name="d">The decay function descriptor.</param>
	/// <param name="field">The field name to apply the decay to.</param>
	/// <param name="origin">The origin point for decay calculation.</param>
	/// <param name="scale">The distance from origin at which the score equals <paramref name="decay"/>.</param>
	/// <param name="offset">Documents within this distance from origin get a score of 1.0.</param>
	/// <param name="decay">The score at <paramref name="scale"/> distance from origin. Default 0.5.</param>
	public static DecayFunctionDescriptor Field(this DecayFunctionDescriptor d,
		string field, object origin, string scale, string? offset = null, double? decay = null)
	{
		var props = new Dictionary<string, object>();
		props["origin"] = origin;
		props["scale"] = scale;
		if (offset is not null) props["offset"] = offset;
		if (decay is not null) props["decay"] = decay.Value;

		d._value.AdditionalProperties ??= new();
		d._value.AdditionalProperties[field] = JsonSerializer.SerializeToElement(props);
		return d;
	}
}
