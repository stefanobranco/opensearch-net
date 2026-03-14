using OpenSearch.CodeGen.Model;
using OpenSearch.CodeGen.OpenApi;
using OpenSearch.CodeGen.Transformer;
using OpenSearch.CodeGen.Renderer;

// Parse arguments
string specDir = args.Length > 1 && args[0] == "--spec-dir" ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Spec");
string outputDir = args.Length > 3 && args[2] == "--output-dir" ? args[3] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenSearch.Client", "Generated"));
string namespacesArg = "indices";

// Parse --namespaces argument (can appear after --spec-dir and --output-dir)
for (int i = 0; i < args.Length - 1; i++)
{
	if (args[i] == "--namespaces")
	{
		namespacesArg = args[i + 1];
		break;
	}
}

var namespaces = namespacesArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// Resolve to absolute paths
specDir = Path.GetFullPath(specDir);
outputDir = Path.GetFullPath(outputDir);

Console.WriteLine($"Spec directory: {specDir}");
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine($"Namespaces: {string.Join(", ", namespaces)}");

// Stage 1: Parse
Console.WriteLine("Parsing OpenAPI specification...");
var spec = OpenApiSpecification.Load(specDir);
Console.WriteLine($"  Loaded {spec.Operations.Count} operations from {spec.NamespaceFiles.Count} namespace files");

// Stage 2: Transform all namespaces (shared TypeMapper for type dedup)
var transformer = new SpecTransformer(spec);
var renderer = new CodeRenderer(outputDir);
var sharedTypeMapper = new TypeMapper(namespaces[0]);
var allResults = new List<TransformResult>();

foreach (var ns in namespaces)
{
	Console.WriteLine($"Transforming namespace '{ns}'...");
	var shapes = transformer.Transform(ns, sharedTypeMapper);
	Console.WriteLine($"  Generated {shapes.Requests.Count} request shapes, {shapes.Enums.Count} enums, {shapes.Objects.Count} object types");
	renderer.RegisterTypes(shapes);
	allResults.Add(shapes);
}

// Stage 3: Render all namespaces (global type lookup for cross-namespace usings)
foreach (var shapes in allResults)
{
	Console.WriteLine($"Rendering C# code for '{shapes.Namespace}'...");
	renderer.Render(shapes);
}

Console.WriteLine("Done!");
