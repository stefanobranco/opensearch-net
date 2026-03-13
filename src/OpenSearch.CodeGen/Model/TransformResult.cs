namespace OpenSearch.CodeGen.Model;

/// <summary>
/// The result of transforming a namespace's operations into shapes.
/// </summary>
public sealed class TransformResult
{
	public required string Namespace { get; init; }
	public required IReadOnlyList<RequestShape> Requests { get; init; }
	public required IReadOnlyList<EnumShape> Enums { get; init; }
	public required IReadOnlyList<ObjectShape> Objects { get; init; }
}
