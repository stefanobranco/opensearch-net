using System.Text;
using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Fixtures for the security plugin's response quirks: multi-type scalar fields
/// (<c>type: [number, string]</c> sends words like "CREATED") and error envelopes on
/// dictionary-shaped GET responses (404 bodies that cannot deserialize as the dictionary).
/// </summary>
public class SecurityResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Create_user_response_reads_string_status()
	{
		var response = Deserialize<CreateUserSecurityResponse>(
			"""{ "status": "CREATED", "message": "'x' created." }""");

		response!.Status.Should().Be("CREATED");
		response.Message.Should().Be("'x' created.");
	}

	[Fact]
	public void Multi_type_scalar_reads_numeric_wire_form_as_text()
	{
		var response = Deserialize<CreateUserSecurityResponse>("""{ "status": 200 }""");

		response!.Status.Should().Be("200");
	}

	[Fact]
	public void Dictionary_response_deserializes_users()
	{
		using var body = new MemoryStream(Encoding.UTF8.GetBytes(
			"""{ "alice": { "backend_roles": ["ops"], "reserved": false } }"""));

		var response = GetUserSecurityEndpoint.Instance.DeserializeResponse(200, "application/json", body, Serializer);

		response.Should().ContainKey("alice");
		response["alice"].BackendRoles.Should().Contain("ops");
	}

	[Fact]
	public void Dictionary_response_is_empty_on_error_status()
	{
		using var body = new MemoryStream(Encoding.UTF8.GetBytes(
			"""{ "status": "NOT_FOUND", "message": "user missing not found." }"""));

		var response = GetUserSecurityEndpoint.Instance.DeserializeResponse(404, "application/json", body, Serializer);

		response.Should().BeEmpty();
	}
}
