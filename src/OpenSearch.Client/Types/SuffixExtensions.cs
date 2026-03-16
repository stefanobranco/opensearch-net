namespace OpenSearch.Client;

/// <summary>
/// Provides a marker extension method for use in field expressions.
/// At runtime this is a no-op; <see cref="FieldExpressionVisitor"/> detects
/// the method call in the expression tree and appends the suffix to the field path.
/// </summary>
public static class SuffixExtensions
{
	/// <summary>
	/// Appends a suffix to the field path (e.g., .Suffix("keyword") → "name.keyword").
	/// Only meaningful inside Field.From expressions.
	/// </summary>
	public static object Suffix(this object obj, string suffix) => obj;
}
