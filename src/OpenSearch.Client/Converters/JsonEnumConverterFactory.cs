using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that handles enums annotated with <see cref="JsonEnumAttribute"/>.
/// Uses <see cref="EnumMemberAttribute.Value"/> for the JSON wire representation when present,
/// falling back to the field name otherwise. Comparison is case-insensitive on read.
/// </summary>
public sealed class JsonEnumConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsEnum && typeToConvert.GetCustomAttribute<JsonEnumAttribute>() is not null;

	/// <inheritdoc />
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var converterType = typeof(JsonEnumConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter)Activator.CreateInstance(converterType)!;
	}

	private sealed class JsonEnumConverter<T> : JsonConverter<T> where T : struct, Enum
	{
		private readonly Dictionary<T, string> _enumToString = [];
		private readonly Dictionary<string, T> _stringToEnum = new(StringComparer.OrdinalIgnoreCase);

		public JsonEnumConverter()
		{
			foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var value = (T)field.GetValue(null)!;
				var memberAttr = field.GetCustomAttribute<EnumMemberAttribute>();
				var name = memberAttr?.Value ?? field.Name;
				_enumToString[value] = name;
				_stringToEnum[name] = value;
			}
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.GetString();
			if (str is not null && _stringToEnum.TryGetValue(str, out var value))
				return value;

			throw new JsonException($"Unable to convert \"{str}\" to {typeof(T).Name}.");
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if (_enumToString.TryGetValue(value, out var str))
				writer.WriteStringValue(str);
			else
				writer.WriteStringValue(value.ToString());
		}
	}
}
