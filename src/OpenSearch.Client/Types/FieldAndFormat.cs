using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A field reference with optional format. OpenSearch accepts either a plain string
/// (<c>"field_name"</c>) or an object (<c>{"field": "name", "format": "epoch_millis"}</c>).
/// </summary>
[JsonConverter(typeof(Converters.FieldAndFormatConverter))]
public sealed class FieldAndFormat
{
	public string Field { get; set; } = default!;
	public string? Format { get; set; }

	public static implicit operator FieldAndFormat(string field) => new() { Field = field };
}
