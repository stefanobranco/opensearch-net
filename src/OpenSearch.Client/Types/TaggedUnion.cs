namespace OpenSearch.Client;

/// <summary>
/// Base class for discriminated unions where each variant is identified by a <typeparamref name="TKind"/> enum.
/// Generated union types inherit from this class and expose strongly-typed factory methods
/// and accessors for each variant.
/// </summary>
/// <typeparam name="TKind">An enum type identifying the active variant.</typeparam>
/// <typeparam name="TValue">The common base type of all variant values.</typeparam>
public abstract class TaggedUnion<TKind, TValue> where TKind : struct, Enum
{
	/// <summary>
	/// The discriminator identifying which variant is active.
	/// </summary>
	public TKind Kind { get; }

	/// <summary>
	/// The value of the active variant.
	/// </summary>
	public TValue Value { get; }

	/// <summary>
	/// Creates a new tagged union with the specified kind and value.
	/// </summary>
	protected TaggedUnion(TKind kind, TValue value)
	{
		Kind = kind;
		Value = value;
	}

	/// <summary>
	/// Returns <c>true</c> if the active variant matches the specified <paramref name="kind"/>.
	/// </summary>
	public bool Is(TKind kind) => Kind.Equals(kind);

	/// <summary>
	/// Returns the value cast to <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The expected type of the variant value.</typeparam>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the value cannot be cast to <typeparamref name="T"/>.
	/// </exception>
	public T Get<T>() where T : TValue
	{
		if (Value is T typed) return typed;
		throw new InvalidOperationException($"Cannot get value as {typeof(T).Name}; current kind is {Kind}.");
	}

	/// <inheritdoc />
	public override string ToString() => $"{Kind}: {Value}";
}
