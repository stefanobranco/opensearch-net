using OpenSearch.CodeGen.OpenApi;
using OpenSearch.CodeGen.Transformer;
using OpenSearch.CodeGen.Renderer;

// Parse arguments
string specDir = args.Length > 1 && args[0] == "--spec-dir" ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Spec");
string outputDir = args.Length > 3 && args[2] == "--output-dir" ? args[3] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenSearch.Client", "Generated"));

// Resolve to absolute paths
specDir = Path.GetFullPath(specDir);
outputDir = Path.GetFullPath(outputDir);

Console.WriteLine($"Spec directory: {specDir}");
Console.WriteLine($"Output directory: {outputDir}");

// Stage 1: Parse
Console.WriteLine("Parsing OpenAPI specification...");
var spec = OpenApiSpecification.Load(specDir);
Console.WriteLine($"  Loaded {spec.Operations.Count} operations from {spec.NamespaceFiles.Count} namespace files");

// Stage 2: Transform
Console.WriteLine("Transforming to shape model...");
var transformer = new SpecTransformer(spec);
var shapes = transformer.Transform("indices");
Console.WriteLine($"  Generated {shapes.Requests.Count} request shapes, {shapes.Enums.Count} enums, {shapes.Objects.Count} object types");

// Stage 3: Render
Console.WriteLine("Rendering C# code...");
var renderer = new CodeRenderer(outputDir);
renderer.Render(shapes);
Console.WriteLine("Done!");
