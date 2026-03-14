namespace OpenSearch.CodeGen.Model;

/// <summary>
/// Base class for all generated type shapes.
/// </summary>
public abstract class Shape
{
	/// <summary>PascalCase class name.</summary>
	public required string ClassName { get; init; }

	/// <summary>Namespace segment (e.g., "Indices", "Common").</summary>
	public required string Namespace { get; init; }

	/// <summary>Description from the spec.</summary>
	public string? Description { get; init; }

	public override string ToString() => $"{Namespace}.{ClassName}";
}

/// <summary>
/// A regular object type with properties.
/// </summary>
public sealed class ObjectShape : Shape
{
	public required IReadOnlyList<Field> Fields { get; init; }
	public TypeRef? AdditionalPropertiesType { get; init; }

	/// <summary>Generic type parameter names (e.g., ["TDocument"]) if any field references a generic parameter.</summary>
	public IReadOnlyList<string> TypeParameters { get; init; } = [];

	/// <summary>Whether this shape is generic.</summary>
	public bool IsGeneric => TypeParameters.Count > 0;
}

/// <summary>
/// An enum type with string variants.
/// </summary>
public sealed class EnumShape : Shape
{
	public required IReadOnlyList<EnumVariant> Variants { get; init; }
}

/// <summary>
/// A single variant of an enum.
/// </summary>
public sealed class EnumVariant
{
	/// <summary>PascalCase member name in C#.</summary>
	public required string Name { get; init; }

	/// <summary>Wire value as a string.</summary>
	public required string WireValue { get; init; }

	public override string ToString() => $"{Name} = \"{WireValue}\"";
}

/// <summary>
/// A property-based tagged union type (e.g., QueryContainer with ~40 optional variant properties
/// where only one should be set at a time).
/// </summary>
public sealed class TaggedUnionShape : Shape
{
	/// <summary>The Kind enum name (e.g., "QueryKind").</summary>
	public required string KindEnumName { get; init; }

	/// <summary>The variants of the union.</summary>
	public required IReadOnlyList<UnionVariant> Variants { get; init; }
}

/// <summary>
/// A single variant of a property-based tagged union.
/// </summary>
public sealed class UnionVariant
{
	/// <summary>PascalCase name for the factory method and Kind enum member.</summary>
	public required string Name { get; init; }

	/// <summary>Wire name (JSON property name).</summary>
	public required string WireName { get; init; }

	/// <summary>The C# type of the variant value.</summary>
	public required TypeRef Type { get; init; }

	/// <summary>Description from the spec.</summary>
	public string? Description { get; init; }

	public override string ToString() => $"{Name}: {Type.ToCSharpPropertyType()}";
}

/// <summary>
/// An API request type — the request class + its endpoint.
/// </summary>
public sealed class RequestShape : Shape
{
	/// <summary>The x-operation-group (e.g., "indices.create").</summary>
	public required string OperationGroup { get; init; }

	/// <summary>All possible HTTP paths for this operation.</summary>
	public required IReadOnlyList<HttpPath> HttpPaths { get; init; }

	/// <summary>The HTTP method (Get, Post, Put, Delete, Head).</summary>
	public required string HttpMethod { get; init; }

	/// <summary>Path parameters.</summary>
	public required IReadOnlyList<Field> PathParams { get; init; }

	/// <summary>Query string parameters.</summary>
	public required IReadOnlyList<Field> QueryParams { get; init; }

	/// <summary>Body fields (properties serialized as JSON body).</summary>
	public required IReadOnlyList<Field> BodyFields { get; init; }

	/// <summary>Whether the body is the raw user document (e.g., index, create — bare type:object with no properties).</summary>
	public bool IsRawBody { get; init; }

	/// <summary>Whether this operation has a request body.</summary>
	public bool HasBody => BodyFields.Count > 0 || IsRawBody;

	/// <summary>Whether this is a HEAD request (response has no body).</summary>
	public bool IsHead => HttpMethod == "Head";

	/// <summary>The endpoint class name (e.g., "CreateEndpoint").</summary>
	public required string EndpointName { get; init; }

	/// <summary>The response shape for this request.</summary>
	public required ResponseShape Response { get; init; }
}

/// <summary>
/// An API response type.
/// </summary>
public sealed class ResponseShape : Shape
{
	public required IReadOnlyList<Field> Fields { get; init; }

	/// <summary>
	/// If the response is a dictionary (e.g., GET /{index} returns Dictionary&lt;string, IndexState&gt;),
	/// this is the value type.
	/// </summary>
	public TypeRef? DictionaryValueType { get; init; }

	/// <summary>Whether this response is a dictionary type.</summary>
	public bool IsDictionary => DictionaryValueType is not null;

	/// <summary>Whether this is a HEAD endpoint where the response is derived from status code only.</summary>
	public bool IsHeadResponse { get; init; }

	/// <summary>Generic type parameter names (e.g., ["TDocument"]) if any field references a generic parameter.</summary>
	public IReadOnlyList<string> TypeParameters { get; init; } = [];

	/// <summary>Whether this shape is generic.</summary>
	public bool IsGeneric => TypeParameters.Count > 0;
}
