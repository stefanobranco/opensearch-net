using FluentAssertions;
using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.OpenApi;
using OpenSearch.CodeGen.Transformer;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace OpenSearch.CodeGen.Tests;

public class TypeMapperTests
{
    private readonly TypeMapper _mapper = new("_common");

    /// <summary>
    /// Creates an OpenApiSchema from a YamlMappingNode using a no-op RefResolver.
    /// </summary>
    private static OpenApiSchema Schema(YamlMappingNode node)
    {
        // Use a temp directory as spec dir; no actual file resolution needed for direct schemas
        var resolver = new RefResolver(Path.GetTempPath());
        return new OpenApiSchema(node, resolver, Path.Combine(Path.GetTempPath(), "test.yaml"));
    }

    /// <summary>
    /// Creates a simple schema with the given type and optional format.
    /// </summary>
    private static OpenApiSchema SimpleSchema(string type, string? format = null)
    {
        var mapping = new YamlMappingNode
        {
            { "type", type }
        };
        if (format is not null)
            mapping.Add("format", format);
        return Schema(mapping);
    }

    // ---------------------------------------------------------------
    // 1. String schema
    // ---------------------------------------------------------------

    [Fact]
    public void Map_StringSchema_ReturnsStringTypeRef()
    {
        var schema = SimpleSchema("string");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("string");
    }

    // ---------------------------------------------------------------
    // 2. Integer schema
    // ---------------------------------------------------------------

    [Fact]
    public void Map_IntegerSchema_ReturnsIntTypeRef()
    {
        var schema = SimpleSchema("integer");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("int");
    }

    // ---------------------------------------------------------------
    // 3. Int64 schema
    // ---------------------------------------------------------------

    [Fact]
    public void Map_Int64Schema_ReturnsLongTypeRef()
    {
        var schema = SimpleSchema("integer", "int64");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("long");
    }

    // ---------------------------------------------------------------
    // 4. Array schema
    // ---------------------------------------------------------------

    [Fact]
    public void Map_ArraySchema_ReturnsListTypeRef()
    {
        var itemsNode = new YamlMappingNode { { "type", "string" } };
        var mapping = new YamlMappingNode
        {
            { "type", "array" },
            { "items", itemsNode }
        };
        var schema = Schema(mapping);
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.List);
        result.CSharpName.Should().Be("List<string>");
        result.ItemType.Should().NotBeNull();
        result.ItemType!.CSharpName.Should().Be("string");
    }

    // ---------------------------------------------------------------
    // 5. Object with additionalProperties (Dictionary)
    // ---------------------------------------------------------------

    [Fact]
    public void Map_ObjectWithAdditionalProperties_ReturnsDictTypeRef()
    {
        var mapping = new YamlMappingNode
        {
            { "type", "object" },
            { "additionalProperties", new YamlMappingNode { { "type", "integer" } } }
        };
        var schema = Schema(mapping);
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Dictionary);
        result.CSharpName.Should().Be("Dictionary<string, int>");
        result.KeyType!.CSharpName.Should().Be("string");
        result.ValueType!.CSharpName.Should().Be("int");
    }

    // ---------------------------------------------------------------
    // 6. Enum schema (via $ref simulation)
    // ---------------------------------------------------------------

    [Fact]
    public void Map_EnumSchemaViaRef_CreatesEnumShape()
    {
        // Build a YAML tree that simulates a $ref to a schema with enum values.
        // We need the schema at "#/components/schemas/HealthStatus" to have enum values.
        var enumNode = new YamlMappingNode
        {
            { "type", "string" },
            { "enum", new YamlSequenceNode(
                new YamlScalarNode("green"),
                new YamlScalarNode("yellow"),
                new YamlScalarNode("red")) }
        };

        // Build a root document with components/schemas/HealthStatus
        var root = new YamlMappingNode
        {
            { "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode
                        {
                            { "HealthStatus", enumNode }
                        }
                    }
                }
            }
        };

        // Write it to a temp file so RefResolver can find it
        var tempDir = Path.Combine(Path.GetTempPath(), "codegen_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.yaml");
            using (var writer = new StreamWriter(filePath))
            {
                var stream = new YamlStream(new YamlDocument(root));
                stream.Save(writer);
            }

            var resolver = new RefResolver(tempDir);
            // The $ref node
            var refNode = new YamlMappingNode
            {
                { "$ref", "#/components/schemas/HealthStatus" }
            };
            var refSchema = new OpenApiSchema(refNode, resolver, filePath);

            var mapper = new TypeMapper("_common");
            var result = mapper.Map(refSchema);

            result.Kind.Should().Be(TypeRefKind.Named);
            result.IsEnum.Should().BeTrue();
            result.CSharpName.Should().Be("HealthStatus");

            mapper.DiscoveredEnums.Should().ContainKey("HealthStatus");
            var enumShape = mapper.DiscoveredEnums["HealthStatus"];
            enumShape.Variants.Should().HaveCount(3);
            enumShape.Variants[0].Name.Should().Be("Green");
            enumShape.Variants[0].WireValue.Should().Be("green");
            enumShape.Variants[1].Name.Should().Be("Yellow");
            enumShape.Variants[2].Name.Should().Be("Red");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // 7. Object with properties (via $ref simulation)
    // ---------------------------------------------------------------

    [Fact]
    public void Map_ObjectWithPropertiesViaRef_CreatesObjectShape()
    {
        var objectNode = new YamlMappingNode
        {
            { "type", "object" },
            { "properties", new YamlMappingNode
                {
                    { "number_of_shards", new YamlMappingNode { { "type", "integer" } } },
                    { "number_of_replicas", new YamlMappingNode { { "type", "integer" } } }
                }
            }
        };

        var root = new YamlMappingNode
        {
            { "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode
                        {
                            { "IndexSettings", objectNode }
                        }
                    }
                }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "codegen_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.yaml");
            using (var writer = new StreamWriter(filePath))
            {
                var stream = new YamlStream(new YamlDocument(root));
                stream.Save(writer);
            }

            var resolver = new RefResolver(tempDir);
            var refNode = new YamlMappingNode
            {
                { "$ref", "#/components/schemas/IndexSettings" }
            };
            var refSchema = new OpenApiSchema(refNode, resolver, filePath);

            var mapper = new TypeMapper("indices");
            var result = mapper.Map(refSchema);

            result.Kind.Should().Be(TypeRefKind.Named);
            result.CSharpName.Should().Be("IndexSettings");

            mapper.DiscoveredObjects.Should().ContainKey("IndexSettings");
            var obj = mapper.DiscoveredObjects["IndexSettings"];
            obj.Fields.Should().HaveCount(2);
            obj.Fields[0].Name.Should().Be("NumberOfShards");
            obj.Fields[0].Type.CSharpName.Should().Be("int");
            obj.Fields[1].Name.Should().Be("NumberOfReplicas");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // 8. Known string aliases (IndexName, Duration, etc.)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("IndexName")]
    [InlineData("Duration")]
    [InlineData("Field")]
    [InlineData("Routing")]
    [InlineData("ScrollId")]
    public void Map_KnownStringAlias_ReturnsStringTypeRef(string aliasName)
    {
        // Build a root with the alias schema and a $ref to it
        var aliasNode = new YamlMappingNode { { "type", "string" } };
        var root = new YamlMappingNode
        {
            { "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode
                        {
                            { aliasName, aliasNode }
                        }
                    }
                }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "codegen_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.yaml");
            using (var writer = new StreamWriter(filePath))
            {
                var stream = new YamlStream(new YamlDocument(root));
                stream.Save(writer);
            }

            var resolver = new RefResolver(tempDir);
            var refNode = new YamlMappingNode
            {
                { "$ref", $"#/components/schemas/{aliasName}" }
            };
            var refSchema = new OpenApiSchema(refNode, resolver, filePath);

            var mapper = new TypeMapper("_common");
            var result = mapper.Map(refSchema);

            result.Kind.Should().Be(TypeRefKind.Primitive);
            result.CSharpName.Should().Be("string");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // 9. VersionNumber alias maps to Long
    // ---------------------------------------------------------------

    [Fact]
    public void Map_VersionNumberAlias_ReturnsLongTypeRef()
    {
        var aliasNode = new YamlMappingNode { { "type", "integer" } };
        var root = new YamlMappingNode
        {
            { "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode
                        {
                            { "VersionNumber", aliasNode }
                        }
                    }
                }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "codegen_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.yaml");
            using (var writer = new StreamWriter(filePath))
            {
                var stream = new YamlStream(new YamlDocument(root));
                stream.Save(writer);
            }

            var resolver = new RefResolver(tempDir);
            var refNode = new YamlMappingNode
            {
                { "$ref", "#/components/schemas/VersionNumber" }
            };
            var refSchema = new OpenApiSchema(refNode, resolver, filePath);

            var mapper = new TypeMapper("_common");
            var result = mapper.Map(refSchema);

            result.Kind.Should().Be(TypeRefKind.Primitive);
            result.CSharpName.Should().Be("long");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // 10. QueryContainer maps to TaggedUnionShape
    // ---------------------------------------------------------------

    [Fact]
    public void Map_QueryContainer_CreatesTaggedUnionShape()
    {
        var queryContainerNode = new YamlMappingNode
        {
            { "type", "object" },
            { "properties", new YamlMappingNode
                {
                    { "match", new YamlMappingNode { { "type", "object" } } },
                    { "term", new YamlMappingNode { { "type", "object" } } },
                    { "bool", new YamlMappingNode { { "type", "object" } } }
                }
            }
        };

        var root = new YamlMappingNode
        {
            { "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode
                        {
                            { "QueryContainer", queryContainerNode }
                        }
                    }
                }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "codegen_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.yaml");
            using (var writer = new StreamWriter(filePath))
            {
                var stream = new YamlStream(new YamlDocument(root));
                stream.Save(writer);
            }

            var resolver = new RefResolver(tempDir);
            var refNode = new YamlMappingNode
            {
                { "$ref", "#/components/schemas/QueryContainer" }
            };
            var refSchema = new OpenApiSchema(refNode, resolver, filePath);

            var mapper = new TypeMapper("_common");
            var result = mapper.Map(refSchema);

            result.Kind.Should().Be(TypeRefKind.Named);
            result.CSharpName.Should().Be("QueryContainer");

            mapper.DiscoveredTaggedUnions.Should().ContainKey("QueryContainer");
            var union = mapper.DiscoveredTaggedUnions["QueryContainer"];
            union.ClassName.Should().Be("QueryContainer");
            union.KindEnumName.Should().Be("QueryKind");
            union.Variants.Should().HaveCount(3);
            union.Variants.Select(v => v.Name).Should().Contain("Match");
            union.Variants.Select(v => v.Name).Should().Contain("Term");
            union.Variants.Select(v => v.Name).Should().Contain("Bool");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // Additional: boolean, number, double schemas
    // ---------------------------------------------------------------

    [Fact]
    public void Map_BooleanSchema_ReturnsBoolTypeRef()
    {
        var schema = SimpleSchema("boolean");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("bool");
    }

    [Fact]
    public void Map_NumberSchema_ReturnsFloatTypeRef()
    {
        var schema = SimpleSchema("number");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("float");
    }

    [Fact]
    public void Map_NumberDoubleSchema_ReturnsDoubleTypeRef()
    {
        var schema = SimpleSchema("number", "double");
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("double");
    }

    [Fact]
    public void Map_ArrayWithNoItems_ReturnsListOfObject()
    {
        var mapping = new YamlMappingNode
        {
            { "type", "array" }
        };
        var schema = Schema(mapping);
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.List);
        result.ItemType!.CSharpName.Should().Be("object");
    }

    [Fact]
    public void Map_ObjectWithUntypedAdditionalProperties_ReturnsDictOfObject()
    {
        var mapping = new YamlMappingNode
        {
            { "type", "object" },
            { "additionalProperties", new YamlScalarNode("true") }
        };
        var schema = Schema(mapping);
        var result = _mapper.Map(schema);

        // additionalProperties: true with no named properties but HasAdditionalProperties → Dict<string, object>
        result.Kind.Should().Be(TypeRefKind.Dictionary);
        result.CSharpName.Should().Be("Dictionary<string, object>");
    }

    [Fact]
    public void Map_EmptyObjectSchema_ReturnsJsonElement()
    {
        var mapping = new YamlMappingNode
        {
            { "type", "object" }
        };
        var schema = Schema(mapping);
        var result = _mapper.Map(schema);

        result.Kind.Should().Be(TypeRefKind.Primitive);
        result.CSharpName.Should().Be("System.Text.Json.JsonElement");
    }
}
