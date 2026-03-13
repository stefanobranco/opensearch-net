namespace OpenSearch.CodeGen.Model;

/// <summary>
/// A field in a generated class (property of an object type, request param, response field, etc.).
/// </summary>
public sealed class Field
{
	/// <summary>PascalCase property name for C#.</summary>
	public required string Name { get; init; }

	/// <summary>snake_case name as it appears on the wire (JSON).</summary>
	public required string WireName { get; init; }

	/// <summary>The C# type of this field.</summary>
	public required TypeRef Type { get; init; }

	/// <summary>Whether the field is required in the schema.</summary>
	public required bool Required { get; init; }

	/// <summary>Description from the spec.</summary>
	public string? Description { get; init; }

	/// <summary>Whether this field is deprecated.</summary>
	public bool Deprecated { get; init; }

	public override string ToString() => $"{Name}: {Type.ToCSharpPropertyType()}";
}
