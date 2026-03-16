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

		// Detect chained .Suffix() calls — unwrap from outermost inward
		// e.g., .Suffix("de").Suffix("raw") → suffix = "de.raw"
		string? suffix = null;
		while (body is MethodCallExpression methodCall
			&& methodCall.Method.DeclaringType == typeof(SuffixExtensions))
		{
			var suffixArg = methodCall.Arguments[^1];
			var segment = EvaluateStringExpression(suffixArg);
			if (segment is not null)
				suffix = suffix is not null ? string.Concat(segment, ".", suffix) : segment;

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

	/// <summary>
	/// Evaluates an expression that should produce a string value.
	/// Handles constants, closure-captured variables, and falls back to compile+invoke.
	/// </summary>
	private static string? EvaluateStringExpression(Expression expr) => expr switch
	{
		ConstantExpression c => c.Value?.ToString(),
		// Closure-captured variable: field access on a constant (the compiler-generated closure object)
		MemberExpression { Expression: ConstantExpression target } m =>
			m.Member switch
			{
				FieldInfo fi => fi.GetValue(target.Value)?.ToString(),
				PropertyInfo pi => pi.GetValue(target.Value)?.ToString(),
				_ => CompileAndInvoke(expr),
			},
		_ => CompileAndInvoke(expr),
	};

	private static string? CompileAndInvoke(Expression expr) =>
		Expression.Lambda<Func<string?>>(
			expr.Type == typeof(string) ? expr : Expression.Convert(expr, typeof(string))
		).Compile().Invoke();

	private static string ResolveSegment(MemberInfo member) =>
		s_segmentCache.GetOrAdd(member, static m =>
		{
			var attr = m.GetCustomAttribute<JsonPropertyNameAttribute>();
			return attr is not null ? attr.Name : JsonNamingPolicy.SnakeCaseLower.ConvertName(m.Name);
		});
}
