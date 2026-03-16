using System.Text.Json;

namespace OpenSearch.Client.Core;

/// <summary>
/// Convenience overloads for SearchRequestDescriptor (non-generic) that accept
/// primitive types where the generated code uses JsonElement?.
/// </summary>
public static class SearchRequestDescriptorConvenienceOverloads
{
	/// <summary>Sets TrackTotalHits with a bool value.</summary>
	public static SearchRequestDescriptor TrackTotalHits(this SearchRequestDescriptor d, bool value)
	{ d._value.TrackTotalHits = JsonSerializer.SerializeToElement(value); return d; }

	/// <summary>Sets TrackTotalHits with an int value (track up to N hits).</summary>
	public static SearchRequestDescriptor TrackTotalHits(this SearchRequestDescriptor d, int value)
	{ d._value.TrackTotalHits = JsonSerializer.SerializeToElement(value); return d; }
}
