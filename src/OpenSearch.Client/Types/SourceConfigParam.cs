using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The query-parameter form of <c>_source</c>: either a boolean (enable/disable source fetching)
/// or a list of fields to return. Construct implicitly, e.g. <c>Source = false</c> or
/// <c>Source = new[] { "title", "price" }</c>.
/// </summary>
[JsonConverter(typeof(SourceConfigParamConverter))]
public sealed class SourceConfigParam
{
	private SourceConfigParam(bool? enabled, IReadOnlyList<string>? fields)
	{
		Enabled = enabled;
		Fields = fields;
	}

	/// <summary>The boolean form, or <c>null</c> when the field-list form is used.</summary>
	public bool? Enabled { get; }

	/// <summary>The field-list form, or <c>null</c> when the boolean form is used.</summary>
	public IReadOnlyList<string>? Fields { get; }

	public static SourceConfigParam Fetch(bool enabled) => new(enabled, null);
	public static SourceConfigParam Include(params string[] fields) => new(null, fields);
	public static SourceConfigParam Include(IEnumerable<string> fields) => new(null, fields.ToList());

	public static implicit operator SourceConfigParam(bool enabled) => Fetch(enabled);
	public static implicit operator SourceConfigParam(string field) => new(null, [field]);
	public static implicit operator SourceConfigParam(string[] fields) => new(null, fields);
	public static implicit operator SourceConfigParam(List<string> fields) => new(null, fields);

	/// <summary>The query-string form: <c>true</c>/<c>false</c> or a comma-separated field list.</summary>
	public override string ToString() =>
		Enabled is { } enabled ? (enabled ? "true" : "false") : string.Join(",", Fields!);
}

public sealed class SourceConfigParamConverter : JsonConverter<SourceConfigParam>
{
	public override SourceConfigParam Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.True:
			case JsonTokenType.False:
				return SourceConfigParam.Fetch(reader.GetBoolean());
			case JsonTokenType.String:
				return reader.GetString()!;
			case JsonTokenType.StartArray:
				var fields = new List<string>();
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					fields.Add(reader.GetString()!);
				return fields;
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for SourceConfigParam.");
		}
	}

	public override void Write(Utf8JsonWriter writer, SourceConfigParam value, JsonSerializerOptions options)
	{
		if (value.Enabled is { } enabled)
		{
			writer.WriteBooleanValue(enabled);
			return;
		}
		writer.WriteStartArray();
		foreach (var field in value.Fields!)
			writer.WriteStringValue(field);
		writer.WriteEndArray();
	}
}
