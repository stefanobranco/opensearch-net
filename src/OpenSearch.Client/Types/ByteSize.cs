using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A byte quantity that the server reports either as a number of bytes or as a human-readable
/// string ("10gb"), depending on the <c>human</c> flag and API. Construct implicitly from either.
/// </summary>
[JsonConverter(typeof(ByteSizeConverter))]
public readonly struct ByteSize
{
	private ByteSize(long? bytes, string? text)
	{
		Bytes = bytes;
		Text = text;
	}

	/// <summary>The numeric byte count, or <c>null</c> when the human-readable form was used.</summary>
	public long? Bytes { get; }

	/// <summary>The human-readable form ("10gb"), or <c>null</c> when the numeric form was used.</summary>
	public string? Text { get; }

	public static implicit operator ByteSize(long bytes) => new(bytes, null);
	public static implicit operator ByteSize(string text) => new(null, text);

	public override string ToString() => Text ?? Bytes?.ToString() ?? "";
}

public sealed class ByteSizeConverter : JsonConverter<ByteSize>
{
	public override ByteSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType switch
		{
			JsonTokenType.Number => reader.GetInt64(),
			JsonTokenType.String => reader.GetString()!,
			_ => throw new JsonException($"Unexpected token {reader.TokenType} for ByteSize."),
		};

	public override void Write(Utf8JsonWriter writer, ByteSize value, JsonSerializerOptions options)
	{
		if (value.Bytes is { } bytes)
			writer.WriteNumberValue(bytes);
		else
			writer.WriteStringValue(value.Text ?? "");
	}
}
