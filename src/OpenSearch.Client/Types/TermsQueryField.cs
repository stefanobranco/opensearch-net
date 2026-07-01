using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The values of a <c>terms</c> query for a field: either an explicit list of values, or a
/// <see cref="TermsLookup"/> that fetches them from another document. Construct implicitly from a
/// value array or a lookup, e.g. <c>TermsQueryField f = new FieldValue[] { "a", "b" };</c>.
/// </summary>
[JsonConverter(typeof(TermsQueryFieldConverter))]
public sealed class TermsQueryField
{
	/// <summary>The explicit-values form, or <c>null</c> when a lookup is used.</summary>
	public IReadOnlyList<FieldValue>? Values { get; }

	/// <summary>The terms-lookup form, or <c>null</c> when explicit values are used.</summary>
	public TermsLookup? Lookup { get; }

	private TermsQueryField(IReadOnlyList<FieldValue> values) => Values = values;
	private TermsQueryField(TermsLookup lookup) => Lookup = lookup;

	public static implicit operator TermsQueryField(FieldValue[] values) => new(values);
	public static implicit operator TermsQueryField(List<FieldValue> values) => new(values);
	public static implicit operator TermsQueryField(TermsLookup lookup) => new(lookup);
}

public sealed class TermsQueryFieldConverter : JsonConverter<TermsQueryField>
{
	public override TermsQueryField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<List<FieldValue>>(ref reader, options)!;
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<TermsLookup>(ref reader, options)
					?? throw new JsonException("Failed to read TermsLookup.");
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for TermsQueryField.");
		}
	}

	public override void Write(Utf8JsonWriter writer, TermsQueryField value, JsonSerializerOptions options)
	{
		if (value.Lookup is not null)
			JsonSerializer.Serialize(writer, value.Lookup, options);
		else
			JsonSerializer.Serialize(writer, value.Values, options);
	}
}
