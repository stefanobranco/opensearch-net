using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The <c>include</c> filter of a <c>terms</c> aggregation. Three forms: a regular-expression string,
/// an explicit list of values, or a <see cref="TermsPartition"/>. Construct implicitly from a string
/// (regex) or a string array (values), e.g. <c>Include = "prod.*"</c> or <c>Include = new[] { "a", "b" }</c>.
/// </summary>
[JsonConverter(typeof(TermsIncludeConverter))]
public sealed class TermsInclude
{
	/// <summary>The regular-expression form, or <c>null</c> when another form is used.</summary>
	public string? Regexp { get; }

	/// <summary>The explicit-values form, or <c>null</c> when another form is used.</summary>
	public IReadOnlyList<string>? Values { get; }

	/// <summary>The partition form, or <c>null</c> when another form is used.</summary>
	public TermsPartition? Partition { get; }

	private TermsInclude(string regexp) => Regexp = regexp;
	private TermsInclude(IReadOnlyList<string> values) => Values = values;
	private TermsInclude(TermsPartition partition) => Partition = partition;

	public static implicit operator TermsInclude(string regexp) => new(regexp);
	public static implicit operator TermsInclude(string[] values) => new(values);
	public static implicit operator TermsInclude(List<string> values) => new(values);
	public static implicit operator TermsInclude(TermsPartition partition) => new(partition);
}

public sealed class TermsIncludeConverter : JsonConverter<TermsInclude>
{
	public override TermsInclude? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return reader.GetString()!;
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<List<string>>(ref reader, options)!;
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<TermsPartition>(ref reader, options)
					?? throw new JsonException("Failed to read TermsPartition.");
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for TermsInclude.");
		}
	}

	public override void Write(Utf8JsonWriter writer, TermsInclude value, JsonSerializerOptions options)
	{
		if (value.Regexp is not null)
			writer.WriteStringValue(value.Regexp);
		else if (value.Partition is not null)
			JsonSerializer.Serialize(writer, value.Partition, options);
		else
			JsonSerializer.Serialize(writer, value.Values, options);
	}
}
