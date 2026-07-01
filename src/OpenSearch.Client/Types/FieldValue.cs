using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A field value whose concrete type depends on the target field's mapping and is only known at
/// runtime — a <c>string</c>, number, or <c>bool</c>. Used by query values such as
/// <see cref="MatchQuery.Query"/> and <see cref="TermQuery.Value"/>.
/// Construct implicitly from a primitive, e.g. <c>new TermQuery { Value = "active" }</c> or
/// <c>new MatchQuery { Query = 42 }</c>.
/// </summary>
[JsonConverter(typeof(FieldValueConverter))]
public readonly struct FieldValue : IEquatable<FieldValue>
{
	internal FieldValue(ValueKind kind, object? value)
	{
		Kind = kind;
		Value = value;
	}

	/// <summary>The kind of value held by this <see cref="FieldValue"/>.</summary>
	public ValueKind Kind { get; }

	/// <summary>The boxed underlying value (<c>null</c>, <see cref="bool"/>, <see cref="long"/>, <see cref="double"/>, or <see cref="string"/>).</summary>
	public object? Value { get; }

	/// <summary>The possible value kinds a <see cref="FieldValue"/> may hold.</summary>
	public enum ValueKind
	{
		Null,
		Double,
		Long,
		Boolean,
		String,
	}

	public static FieldValue Null { get; } = new(ValueKind.Null, null);
	public static FieldValue True { get; } = new(ValueKind.Boolean, true);
	public static FieldValue False { get; } = new(ValueKind.Boolean, false);

	public static FieldValue Long(long value) => new(ValueKind.Long, value);
	public static FieldValue Double(double value) => new(ValueKind.Double, value);
	public static FieldValue Boolean(bool value) => value ? True : False;
	public static FieldValue String(string value) => new(ValueKind.String, value);

	public bool IsNull => Kind is ValueKind.Null;
	public bool IsString => Kind is ValueKind.String;
	public bool IsBool => Kind is ValueKind.Boolean;
	public bool IsLong => Kind is ValueKind.Long;
	public bool IsDouble => Kind is ValueKind.Double;

	public bool TryGetString([NotNullWhen(true)] out string? value)
	{
		value = IsString ? (string)Value! : null;
		return IsString;
	}

	public bool TryGetBool([NotNullWhen(true)] out bool? value)
	{
		value = IsBool ? (bool)Value! : null;
		return IsBool;
	}

	public bool TryGetLong([NotNullWhen(true)] out long? value)
	{
		value = IsLong ? (long)Value! : null;
		return IsLong;
	}

	public bool TryGetDouble([NotNullWhen(true)] out double? value)
	{
		value = IsDouble ? (double)Value! : null;
		return IsDouble;
	}

	public override string ToString() =>
		Kind switch
		{
			ValueKind.Null => "null",
			ValueKind.Double => ((double)Value!).ToString(CultureInfo.InvariantCulture),
			ValueKind.Long => ((long)Value!).ToString(CultureInfo.InvariantCulture),
			ValueKind.Boolean => (bool)Value! ? "true" : "false",
			ValueKind.String => (string)Value!,
			_ => throw new InvalidOperationException($"Unknown FieldValue kind '{Kind}'."),
		};

	public bool Equals(FieldValue other) => Kind == other.Kind && EqualityComparer<object?>.Default.Equals(Value, other.Value);
	public override bool Equals(object? obj) => obj is FieldValue other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Kind, Value);

	public static bool operator ==(FieldValue left, FieldValue right) => left.Equals(right);
	public static bool operator !=(FieldValue left, FieldValue right) => !left.Equals(right);

	public static implicit operator FieldValue(string value) => String(value);
	public static implicit operator FieldValue(bool value) => Boolean(value);
	public static implicit operator FieldValue(int value) => Long(value);
	public static implicit operator FieldValue(long value) => Long(value);
	public static implicit operator FieldValue(double value) => Double(value);
	public static implicit operator FieldValue(float value) => Double(value);
}
