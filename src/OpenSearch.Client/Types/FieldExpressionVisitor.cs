using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Walks an <see cref="Expression{TDelegate}"/> tree to resolve a field path string.
/// Uses [JsonPropertyName] attributes when present, otherwise applies snake_case naming.
/// Per-member segment names are cached to avoid repeated reflection.
/// </summary>
internal static class FieldExpressionVisitor
{
	private static readonly ConcurrentDictionary<MemberInfo, string> s_segmentCache = new();

	public static string Resolve<T>(Expression<Func<T, object>> expression)
	{
		var body = expression.Body;

		// Strip Convert (boxing value types to object)
		if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
			body = unary.Operand;

		// Detect .Suffix("keyword") calls — check DeclaringType to avoid false positives
		string? suffix = null;
		if (body is MethodCallExpression methodCall
			&& methodCall.Method.DeclaringType == typeof(SuffixExtensions))
		{
			var suffixArg = methodCall.Arguments[^1];
			if (suffixArg is ConstantExpression constExpr)
				suffix = constExpr.Value?.ToString();

			body = methodCall.Arguments[0];

			if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary2)
				body = unary2.Operand;
		}

		// Walk MemberExpression chain, collecting segments directly
		// Members are visited leaf-to-root, so we build in reverse
		int depth = 0;
		var current = body;
		while (current is MemberExpression) { depth++; current = ((MemberExpression)current).Expression!; }

		if (depth == 0)
			return suffix ?? string.Empty;

		var segments = new string[depth];
		current = body;
		for (int i = depth - 1; i >= 0; i--)
		{
			var member = (MemberExpression)current!;
			segments[i] = ResolveSegment(member.Member);
			current = member.Expression;
		}

		var result = string.Join(".", segments);
		return suffix is not null ? string.Concat(result, ".", suffix) : result;
	}

	private static string ResolveSegment(MemberInfo member) =>
		s_segmentCache.GetOrAdd(member, static m =>
		{
			var attr = m.GetCustomAttribute<JsonPropertyNameAttribute>();
			return attr is not null ? attr.Name : JsonNamingPolicy.SnakeCaseLower.ConvertName(m.Name);
		});
}
