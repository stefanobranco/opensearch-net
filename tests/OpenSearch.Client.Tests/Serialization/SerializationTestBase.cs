using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Shared scaffolding for STJ serialization fixtures: the canonical client options plus
/// serialize/deserialize/round-trip helpers, so each fixture file stays focused on the wire
/// format of the type under test. Mirrors the inline options used by the older serialization
/// tests (SnakeCaseLower + omit-null + read-numbers-from-string + enum factory).
/// </summary>
public abstract class SerializationTestBase
{
	protected static readonly JsonSerializerOptions Json = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		options.Converters.Add(new JsonEnumConverterFactory());
		return options;
	}

	protected static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

	protected static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Json);

	protected static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

	/// <summary>Wraps a CLR value as a <see cref="JsonElement"/> for properties typed as JsonElement.</summary>
	protected static JsonElement Element(object? value) => JsonSerializer.SerializeToElement(value, Json);

	/// <summary>
	/// Serializes, deserializes, and re-serializes <paramref name="value"/>, asserting the two
	/// JSON renderings are byte-identical (canonical round-trip), and returns the parsed JSON.
	/// </summary>
	protected static JsonElement AssertRoundTrips<T>(T value)
	{
		var first = Serialize(value);
		var back = Deserialize<T>(first);
		var second = Serialize(back);
		second.Should().Be(first, "deserialize→serialize should reproduce the original JSON");
		return Parse(first);
	}

	/// <summary>
	/// Asserts a field-keyed query shape — <c>{ "&lt;kind&gt;": { "&lt;field&gt;": { ... } } }</c> — and
	/// returns the inner value object for further assertions.
	/// </summary>
	protected static JsonElement AssertFieldKeyed(JsonElement root, string kind, string field)
	{
		root.TryGetProperty(kind, out var kindEl).Should().BeTrue($"expected a '{kind}' query wrapper");
		kindEl.TryGetProperty(field, out var fieldEl).Should().BeTrue($"expected field key '{field}' under '{kind}'");
		return fieldEl;
	}
}
