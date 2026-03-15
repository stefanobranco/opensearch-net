using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// Represents a field name in an OpenSearch query. Supports construction from strings
/// or from expression trees with compile-time safety.
/// </summary>
[JsonConverter(typeof(FieldConverter))]
public sealed class Field
{
	public string Name { get; }

	public Field(string name)
	{
		ArgumentNullException.ThrowIfNull(name);
		Name = name;
	}

	/// <summary>
	/// Creates a Field from a lambda expression, resolving member names using
	/// [JsonPropertyName] attributes and snake_case naming policy.
	/// </summary>
	public static Field From<T>(Expression<Func<T, object>> expression) =>
		new(FieldExpressionVisitor.Resolve(expression));

	/// <summary>
	/// Returns a new Field with the given suffix appended (e.g., "name" + "keyword" → "name.keyword").
	/// </summary>
	public Field Suffix(string suffix) => new($"{Name}.{suffix}");

	public static implicit operator Field(string name) => new(name);
	public static implicit operator string(Field field) => field.Name;

	public override string ToString() => Name;
	public override bool Equals(object? obj) => obj is Field other && Name == other.Name;
	public override int GetHashCode() => Name.GetHashCode();
}
