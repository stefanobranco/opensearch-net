namespace OpenSearch.CodeGen.Model;

/// <summary>
/// A reference to a type in the generated code.
/// </summary>
public sealed class TypeRef
{
	public TypeRefKind Kind { get; init; }
	public string Name { get; init; } = "";
	public string CSharpName { get; init; } = "";
	public TypeRef? ItemType { get; init; }
	public TypeRef? KeyType { get; init; }
	public TypeRef? ValueType { get; init; }
	public bool IsNullable { get; init; }

	/// <summary>
	/// The shape this type refers to, if it's a named type.
	/// Set during the transform phase.
	/// </summary>
	public Shape? Shape { get; set; }

	// Primitive factories
	public static TypeRef String() => new() { Kind = TypeRefKind.Primitive, Name = "string", CSharpName = "string" };
	public static TypeRef Bool() => new() { Kind = TypeRefKind.Primitive, Name = "bool", CSharpName = "bool" };
	public static TypeRef Int() => new() { Kind = TypeRefKind.Primitive, Name = "int", CSharpName = "int" };
	public static TypeRef Long() => new() { Kind = TypeRefKind.Primitive, Name = "long", CSharpName = "long" };
	public static TypeRef Float() => new() { Kind = TypeRefKind.Primitive, Name = "float", CSharpName = "float" };
	public static TypeRef Double() => new() { Kind = TypeRefKind.Primitive, Name = "double", CSharpName = "double" };
	public static TypeRef Object() => new() { Kind = TypeRefKind.Primitive, Name = "object", CSharpName = "object" };
	public static TypeRef JsonElement() => new() { Kind = TypeRefKind.Primitive, Name = "JsonElement", CSharpName = "System.Text.Json.JsonElement" };
	public static TypeRef Void() => new() { Kind = TypeRefKind.Void, Name = "void", CSharpName = "void" };

	public static TypeRef ListOf(TypeRef itemType) => new()
	{
		Kind = TypeRefKind.List,
		Name = $"List<{itemType.Name}>",
		CSharpName = $"List<{itemType.CSharpName}>",
		ItemType = itemType
	};

	public static TypeRef DictOf(TypeRef keyType, TypeRef valueType) => new()
	{
		Kind = TypeRefKind.Dictionary,
		Name = $"Dictionary<{keyType.Name}, {valueType.Name}>",
		CSharpName = $"Dictionary<{keyType.CSharpName}, {valueType.CSharpName}>",
		KeyType = keyType,
		ValueType = valueType
	};

	public static TypeRef Named(string name, string csharpName) => new()
	{
		Kind = TypeRefKind.Named,
		Name = name,
		CSharpName = csharpName
	};

	public TypeRef WithNullable(bool nullable) => new()
	{
		Kind = Kind,
		Name = Name,
		CSharpName = CSharpName,
		ItemType = ItemType,
		KeyType = KeyType,
		ValueType = ValueType,
		IsNullable = nullable,
		Shape = Shape
	};

	/// <summary>
	/// Returns the C# type string, including ? for nullable value types.
	/// </summary>
	public string ToCSharpString()
	{
		var name = CSharpName;
		if (IsNullable && IsValueType)
			name += "?";
		return name;
	}

	/// <summary>
	/// Returns the C# type string for a property (nullable reference types get ?).
	/// </summary>
	public string ToCSharpPropertyType()
	{
		var name = CSharpName;
		if (IsNullable)
			name += "?";
		return name;
	}

	private bool IsValueType => Kind == TypeRefKind.Primitive && Name is "bool" or "int" or "long" or "float" or "double";

	public override string ToString() => CSharpName;
}

public enum TypeRefKind
{
	Primitive,
	List,
	Dictionary,
	Named,
	Void
}
