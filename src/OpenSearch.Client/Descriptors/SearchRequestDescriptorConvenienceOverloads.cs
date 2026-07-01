namespace OpenSearch.Client;

/// <summary>
/// Convenience overloads for SearchRequestDescriptor (non-generic). <c>TrackTotalHits</c> is a
/// <see cref="TrackHits"/> that converts implicitly from a bool or int, so these just forward the value.
/// </summary>
public static class SearchRequestDescriptorConvenienceOverloads
{
	/// <summary>Track the exact total (<c>true</c>) or disable total tracking (<c>false</c>).</summary>
	public static SearchRequestDescriptor TrackTotalHits(this SearchRequestDescriptor d, bool value)
	{ d._value.TrackTotalHits = value; return d; }

	/// <summary>Track the total exactly up to the given threshold.</summary>
	public static SearchRequestDescriptor TrackTotalHits(this SearchRequestDescriptor d, int value)
	{ d._value.TrackTotalHits = value; return d; }
}
