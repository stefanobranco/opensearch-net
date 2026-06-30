using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Matches documents with a field value within a range. The spec models this as
/// <c>oneOf[NumberRangeQuery, DateRangeQuery]</c> — variants that differ only in the type of their
/// bounds (a number vs a date-math string). This is the merged flat form: the bounds are typed as
/// <see cref="object"/> so a single type accepts both, e.g. <c>Gte = 10</c> or <c>Gte = "now-1d"</c>.
/// </summary>
public sealed class RangeQuery
{
	/// <summary>Floating point number used to decrease or increase the relevance scores of the query.</summary>
	public float? Boost { get; set; }

	[JsonPropertyName("_name")]
	public string? Name { get; set; }

	/// <summary>How the range query matches values for range fields.</summary>
	public RangeRelation? Relation { get; set; }

	/// <summary>Greater than. A number or a date-math expression.</summary>
	public object? Gt { get; set; }

	/// <summary>Greater than or equal to. A number or a date-math expression.</summary>
	public object? Gte { get; set; }

	/// <summary>Less than. A number or a date-math expression.</summary>
	public object? Lt { get; set; }

	/// <summary>Less than or equal to. A number or a date-math expression.</summary>
	public object? Lte { get; set; }

	public string? From { get; set; }

	public string? To { get; set; }

	/// <summary>Date format used to convert date values in the query (date ranges only).</summary>
	public string? Format { get; set; }

	/// <summary>Time zone used to convert date values in the query (date ranges only).</summary>
	public string? TimeZone { get; set; }

	/// <summary>Include the lower bound.</summary>
	public bool? IncludeLower { get; set; }

	/// <summary>Include the upper bound.</summary>
	public bool? IncludeUpper { get; set; }
}
