using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// An index's segment-merge policy: either a built-in policy <see cref="IndexSettingsMergePolicyName"/>
/// (e.g. <c>tiered</c>) or an inline <see cref="IndexSettingsMergeTieredPolicy"/> configuration. Construct
/// implicitly from either, e.g. <c>MergePolicy = IndexSettingsMergePolicyName.Tiered</c>.
/// </summary>
[JsonConverter(typeof(IndexSettingsMergePolicyConverter))]
public sealed class IndexSettingsMergePolicy
{
	/// <summary>The named-policy form, or <c>null</c> when an inline config is used.</summary>
	public IndexSettingsMergePolicyName? Name { get; }

	/// <summary>The inline tiered-policy form, or <c>null</c> when a named policy is used.</summary>
	public IndexSettingsMergeTieredPolicy? Tiered { get; }

	private IndexSettingsMergePolicy(IndexSettingsMergePolicyName name) => Name = name;
	private IndexSettingsMergePolicy(IndexSettingsMergeTieredPolicy tiered) => Tiered = tiered;

	public static implicit operator IndexSettingsMergePolicy(IndexSettingsMergePolicyName name) => new(name);
	public static implicit operator IndexSettingsMergePolicy(IndexSettingsMergeTieredPolicy tiered) => new(tiered);
}

public sealed class IndexSettingsMergePolicyConverter : JsonConverter<IndexSettingsMergePolicy>
{
	public override IndexSettingsMergePolicy? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.String:
				return JsonSerializer.Deserialize<IndexSettingsMergePolicyName>(ref reader, options);
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<IndexSettingsMergeTieredPolicy>(ref reader, options)
					?? throw new JsonException("Failed to read IndexSettingsMergeTieredPolicy.");
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for IndexSettingsMergePolicy.");
		}
	}

	public override void Write(Utf8JsonWriter writer, IndexSettingsMergePolicy value, JsonSerializerOptions options)
	{
		if (value.Tiered is not null)
			JsonSerializer.Serialize(writer, value.Tiered, options);
		else
			JsonSerializer.Serialize(writer, value.Name, options);
	}
}
