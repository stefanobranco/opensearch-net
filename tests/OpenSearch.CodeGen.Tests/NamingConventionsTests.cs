using FluentAssertions;
using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.Transformer;
using Xunit;

namespace OpenSearch.CodeGen.Tests;

public class NamingConventionsTests
{
    [Theory]
    [InlineData("number_of_shards", "NumberOfShards")]
    [InlineData("_id", "Id")]
    [InlineData("x-custom-header", "XCustomHeader")]
    [InlineData("cluster.health", "ClusterHealth")]
    [InlineData("", "")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected) =>
        NamingConventions.ToPascalCase(input).Should().Be(expected);

    [Theory]
    [InlineData("1xx", "_1xx")]
    [InlineData("2xx", "_2xx")]
    public void ToPascalCase_PrependUnderscore_WhenStartsWithDigit(string input, string expected) =>
        NamingConventions.ToPascalCase(input).Should().Be(expected);

    [Fact]
    public void SchemaNameToClassName_StripsDottedPrefix() =>
        NamingConventions.SchemaNameToClassName("indices._common.Alias").Should().Be("Alias");

    [Fact]
    public void SchemaNameToClassName_HandlesSimpleName() =>
        NamingConventions.SchemaNameToClassName("ShardStatistics").Should().Be("ShardStatistics");

    [Fact]
    public void SchemaNameToClassName_RenamesAction() =>
        NamingConventions.SchemaNameToClassName("Action").Should().Be("IndexAction");

    [Theory]
    [InlineData("open", "Open")]
    [InlineData("read_only", "ReadOnly")]
    [InlineData("1xx", "_1xx")]
    [InlineData("green", "Green")]
    public void EnumValueToMemberName_ConvertsCorrectly(string wireValue, string expected) =>
        NamingConventions.EnumValueToMemberName(wireValue).Should().Be(expected);

    [Fact]
    public void FixFieldNameClash_RenamesClashingField()
    {
        var fields = new List<Field>
        {
            new() { Name = "Query", WireName = "query", Type = TypeRef.String(), Required = false },
            new() { Name = "Size", WireName = "size", Type = TypeRef.Int(), Required = false }
        };
        NamingConventions.FixFieldNameClash(fields, "Query");
        fields[0].Name.Should().Be("QueryValue");
        fields[1].Name.Should().Be("Size");
    }

    [Fact]
    public void FixFieldNameClash_DoesNotRenameNonClashingFields()
    {
        var fields = new List<Field>
        {
            new() { Name = "Query", WireName = "query", Type = TypeRef.String(), Required = false },
            new() { Name = "Size", WireName = "size", Type = TypeRef.Int(), Required = false }
        };
        NamingConventions.FixFieldNameClash(fields, "SearchRequest");
        fields[0].Name.Should().Be("Query");
        fields[1].Name.Should().Be("Size");
    }

    [Fact]
    public void OperationGroupToNames_ProducesCorrectNames()
    {
        var (req, resp, ep) = NamingConventions.OperationGroupToNames("indices.create");
        req.Should().Be("CreateRequest");
        resp.Should().Be("CreateResponse");
        ep.Should().Be("CreateEndpoint");
    }

    [Fact]
    public void OperationGroupToNames_SingleSegment()
    {
        var (req, resp, ep) = NamingConventions.OperationGroupToNames("search");
        req.Should().Be("SearchRequest");
        resp.Should().Be("SearchResponse");
        ep.Should().Be("SearchEndpoint");
    }

    [Fact]
    public void OperationGroupToMethodName_ReturnsAction() =>
        NamingConventions.OperationGroupToMethodName("indices.get_alias").Should().Be("GetAlias");

    [Fact]
    public void OperationGroupToMethodName_SingleSegment() =>
        NamingConventions.OperationGroupToMethodName("search").Should().Be("Search");

    [Fact]
    public void NamespaceToClassName_StripsUnderscore() =>
        NamingConventions.NamespaceToClassName("_core").Should().Be("Core");

    [Fact]
    public void NamespaceToClassName_RegularNamespace() =>
        NamingConventions.NamespaceToClassName("indices").Should().Be("Indices");

    [Theory]
    [InlineData("number_of_shards", "NumberOfShards", false)]
    [InlineData("_id", "Id", true)]
    [InlineData("x-custom-header", "XCustomHeader", true)]
    public void NeedsJsonPropertyName_DetectsNonRoundTrippableNames(string wireName, string pascalName, bool expected) =>
        NamingConventions.NeedsJsonPropertyName(wireName, pascalName).Should().Be(expected);

    [Fact]
    public void SanitizeIdentifier_PrependUnderscoreForDigit() =>
        NamingConventions.SanitizeIdentifier("123abc").Should().Be("_123abc");

    [Fact]
    public void SanitizeIdentifier_EmptyString() =>
        NamingConventions.SanitizeIdentifier("").Should().Be("_");

    [Fact]
    public void SanitizeIdentifier_ValidIdentifier() =>
        NamingConventions.SanitizeIdentifier("validName").Should().Be("validName");
}
