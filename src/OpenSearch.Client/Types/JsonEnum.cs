namespace OpenSearch.Client;

/// <summary>
/// Marks an enum type for custom JSON serialization using <see cref="JsonEnumConverterFactory"/>.
/// Enum members annotated with <see cref="System.Runtime.Serialization.EnumMemberAttribute"/>
/// will use the attribute's <c>Value</c> as the serialized string.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class JsonEnumAttribute : Attribute;

/// <summary>
/// Marker interface for generated enum wrapper types that need string-based serialization.
/// Implementations expose their serialized wire value via <see cref="Value"/>.
/// </summary>
public interface IJsonEnum
{
	/// <summary>
	/// The string value used on the wire (JSON).
	/// </summary>
	string Value { get; }
}
