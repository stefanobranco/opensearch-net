using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Net;

namespace OpenSearch.Client;

/// <summary>
/// Handles deserialization of OpenSearch error responses, which can appear in two forms:
/// <list type="bullet">
///   <item><c>{ "error": { "type": "...", "reason": "..." }, "status": 404 }</c></item>
///   <item><c>{ "error": "string message", "status": 404 }</c></item>
/// </list>
/// </summary>
public sealed class ServerErrorConverter : JsonConverter<ServerError>
{
	/// <inheritdoc />
	public override ServerError? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected start of object for {nameof(ServerError)}, got {reader.TokenType}.");

		var response = new ServerError();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
				continue;

			var propertyName = reader.GetString();
			reader.Read(); // Move to value

			switch (propertyName)
			{
				case "error":
					response.Error = ReadError(ref reader, options);
					break;

				case "status":
					response.Status = reader.GetInt32();
					break;

				default:
					reader.Skip();
					break;
			}
		}

		return response;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, ServerError value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (value.Error is not null)
		{
			writer.WritePropertyName("error");
			WriteErrorCause(writer, value.Error, options);
		}

		writer.WriteNumber("status", value.Status);
		writer.WriteEndObject();
	}

	private static ErrorCause? ReadError(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
			{
				// Plain string error: { "error": "some message" }
				var message = reader.GetString();
				return new ErrorCause { Reason = message };
			}

			case JsonTokenType.StartObject:
			{
				return ReadErrorCauseObject(ref reader, options);
			}

			default:
				reader.Skip();
				return null;
		}
	}

	private static ErrorCause ReadErrorCauseObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		var error = new ErrorCause();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
				continue;

			var prop = reader.GetString();
			reader.Read();

			switch (prop)
			{
				case "type":
					error.Type = reader.GetString();
					break;

				case "reason":
					error.Reason = reader.GetString();
					break;

				case "stack_trace":
					error.StackTrace = reader.GetString();
					break;

				case "caused_by":
					error.CausedBy = reader.TokenType == JsonTokenType.StartObject
						? ReadErrorCauseObject(ref reader, options)
						: null;
					break;

				case "root_cause":
					if (reader.TokenType == JsonTokenType.StartArray)
					{
						error.RootCause = [];
						while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
						{
							if (reader.TokenType == JsonTokenType.StartObject)
								error.RootCause.Add(ReadErrorCauseObject(ref reader, options));
							else
								reader.Skip();
						}
					}
					else
					{
						reader.Skip();
					}
					break;

				default:
					reader.Skip();
					break;
			}
		}

		return error;
	}

	private static void WriteErrorCause(Utf8JsonWriter writer, ErrorCause error, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		if (error.Type is not null)
			writer.WriteString("type", error.Type);

		if (error.Reason is not null)
			writer.WriteString("reason", error.Reason);

		if (error.StackTrace is not null)
			writer.WriteString("stack_trace", error.StackTrace);

		if (error.CausedBy is not null)
		{
			writer.WritePropertyName("caused_by");
			WriteErrorCause(writer, error.CausedBy, options);
		}

		if (error.RootCause is { Count: > 0 })
		{
			writer.WritePropertyName("root_cause");
			writer.WriteStartArray();
			foreach (var cause in error.RootCause)
				WriteErrorCause(writer, cause, options);
			writer.WriteEndArray();
		}

		writer.WriteEndObject();
	}
}
