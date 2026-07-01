using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// <c>track_total_hits</c>: either a boolean (<c>true</c> tracks the exact total, <c>false</c> disables
/// tracking) or an integer accuracy threshold up to which the total is tracked exactly. Construct
/// implicitly, e.g. <c>TrackTotalHits = true</c> or <c>TrackTotalHits = 10_000</c>.
/// </summary>
[JsonConverter(typeof(TrackHitsConverter))]
public readonly struct TrackHits
{
	private TrackHits(bool? enabled, int? count)
	{
		Enabled = enabled;
		Count = count;
	}

	/// <summary>The boolean form, or <c>null</c> when the threshold form is used.</summary>
	public bool? Enabled { get; }

	/// <summary>The accuracy-threshold form, or <c>null</c> when the boolean form is used.</summary>
	public int? Count { get; }

	public static TrackHits Track(bool enabled) => new(enabled, null);
	public static TrackHits Threshold(int count) => new(null, count);

	public static implicit operator TrackHits(bool enabled) => new(enabled, null);
	public static implicit operator TrackHits(int count) => new(null, count);
}

public sealed class TrackHitsConverter : JsonConverter<TrackHits>
{
	public override TrackHits Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType switch
		{
			JsonTokenType.True => TrackHits.Track(true),
			JsonTokenType.False => TrackHits.Track(false),
			JsonTokenType.Number => TrackHits.Threshold(reader.GetInt32()),
			_ => throw new JsonException($"Unexpected token {reader.TokenType} for TrackHits."),
		};

	public override void Write(Utf8JsonWriter writer, TrackHits value, JsonSerializerOptions options)
	{
		if (value.Enabled is { } enabled)
			writer.WriteBooleanValue(enabled);
		else if (value.Count is { } count)
			writer.WriteNumberValue(count);
		else
			writer.WriteBooleanValue(false);
	}
}
