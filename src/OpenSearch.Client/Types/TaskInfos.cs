using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// The <c>tasks</c> field of a task-list response. Its shape depends on the request's <c>group_by</c>:
/// a flat list of <see cref="TaskInfo"/> (<c>group_by=none</c>), or a map of parent task id to
/// <see cref="TaskGroup"/> (<c>group_by=parents|nodes</c>). Check <see cref="Flat"/> / <see cref="Grouped"/>.
/// </summary>
[JsonConverter(typeof(TaskInfosConverter))]
public sealed class TaskInfos
{
	/// <summary>The flat-list form (<c>group_by=none</c>), or <c>null</c> when grouped.</summary>
	public IReadOnlyList<TaskInfo>? Flat { get; }

	/// <summary>The grouped form (<c>group_by=parents|nodes</c>), or <c>null</c> when flat.</summary>
	public IReadOnlyDictionary<string, TaskGroup>? Grouped { get; }

	private TaskInfos(IReadOnlyList<TaskInfo> flat) => Flat = flat;
	private TaskInfos(IReadOnlyDictionary<string, TaskGroup> grouped) => Grouped = grouped;

	public static TaskInfos FromList(IReadOnlyList<TaskInfo> flat) => new(flat);
	public static TaskInfos FromMap(IReadOnlyDictionary<string, TaskGroup> grouped) => new(grouped);
}

public sealed class TaskInfosConverter : JsonConverter<TaskInfos>
{
	public override TaskInfos? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.StartArray:
				return TaskInfos.FromList(JsonSerializer.Deserialize<List<TaskInfo>>(ref reader, options)!);
			case JsonTokenType.StartObject:
				return TaskInfos.FromMap(JsonSerializer.Deserialize<Dictionary<string, TaskGroup>>(ref reader, options)!);
			default:
				throw new JsonException($"Unexpected token {reader.TokenType} for TaskInfos.");
		}
	}

	public override void Write(Utf8JsonWriter writer, TaskInfos value, JsonSerializerOptions options)
	{
		if (value.Grouped is not null)
			JsonSerializer.Serialize(writer, value.Grouped, options);
		else
			JsonSerializer.Serialize(writer, value.Flat, options);
	}
}
