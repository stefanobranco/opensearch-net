using FluentAssertions;
using Xunit;

namespace OpenSearch.Client.Tests;

/// <summary>
/// Response fixture for ism.get_policy, whose response schema is a <c>$ref</c>-alias chain
/// (<c>GetPolicyResponse → PolicyWithMetadata</c>). Following the chain gives it the metadata-wrapped
/// policy shape; previously it was an empty type that discarded the payload.
/// </summary>
public class IsmResponseSerializationTests : SerializationTestBase
{
	[Fact]
	public void Get_policy_response_deserializes_metadata_wrapped_policy()
	{
		const string json = """
		{
		  "_id": "rollover-policy",
		  "_version": 2,
		  "_seq_no": 10,
		  "_primary_term": 1,
		  "policy": {
		    "description": "rollover after 7d",
		    "default_state": "hot"
		  }
		}
		""";

		var response = Deserialize<GetPolicyIsmResponse>(json);

		response!.Id.Should().Be("rollover-policy");
		response.Version.Should().Be(2);
		response.SeqNo.Should().Be(10);
		response.Policy!.Description.Should().Be("rollover after 7d");
		response.Policy.DefaultState.Should().Be("hot");
	}
}
