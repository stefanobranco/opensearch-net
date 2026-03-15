using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Walks an <see cref="Expression{TDelegate}"/> tree to resolve a field path string.
/// Uses [JsonPropertyName] attributes when present, otherwise applies snake_case naming.
/// </summary>
internal static class FieldExpressionVisitor
{
	public static string Resolve<T>(Expression<Func<T, object>> expression)
	{
		var body = expression.Body;

		// Strip Convert (boxing value types to object)
		if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
			body = unary.Operand;

		// Detect .Suffix("keyword") calls
		string? suffix = null;
		if (body is MethodCallExpression methodCall && methodCall.Method.Name == "Suffix")
		{
			// Extension method: SuffixExtensions.Suffix(obj, suffix)
			// Arguments[0] is the instance, Arguments[1] (or last) is the suffix string
			var suffixArg = methodCall.Arguments[^1];
			if (suffixArg is ConstantExpression constExpr)
				suffix = constExpr.Value?.ToString();

			body = methodCall.Arguments[0];

			// Strip another Convert if present
			if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary2)
				body = unary2.Operand;
		}

		// Walk MemberExpression chain
		var members = new List<MemberInfo>();
		while (body is MemberExpression member)
		{
			members.Add(member.Member);
			body = member.Expression!;
		}

		// Members are collected leaf-to-root, reverse for path order
		members.Reverse();

		var segments = new List<string>(members.Count);
		foreach (var member in members)
		{
			var jsonPropName = member.GetCustomAttribute<JsonPropertyNameAttribute>();
			if (jsonPropName is not null)
				segments.Add(jsonPropName.Name);
			else
				segments.Add(JsonNamingPolicy.SnakeCaseLower.ConvertName(member.Name));
		}

		var result = string.Join(".", segments);
		if (suffix is not null)
			result += "." + suffix;

		return result;
	}
}
