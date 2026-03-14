using OpenSearch.CodeGen.Model;
using Scriban;
using Scriban.Runtime;

namespace OpenSearch.CodeGen.Renderer;

/// <summary>
/// Renders shapes into C# source files using Scriban templates.
/// </summary>
public sealed class CodeRenderer
{
	private readonly string _outputDir;
	private readonly TemplateLoader _templates;
	private readonly HashSet<string> _renderedTypes = new(StringComparer.Ordinal);

	public CodeRenderer(string outputDir)
	{
		_outputDir = outputDir;
		// Templates are next to the CodeGen project
		var templatesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates");
		templatesDir = Path.GetFullPath(templatesDir);
		_templates = new TemplateLoader(templatesDir);
	}

	private readonly Dictionary<string, ObjectShape> _globalObjects = new(StringComparer.Ordinal);
	private readonly Dictionary<string, TaggedUnionShape> _globalUnions = new(StringComparer.Ordinal);

	/// <summary>
	/// Registers all types from a <see cref="TransformResult"/> into the global type lookup.
	/// Must be called for ALL namespaces before any <see cref="Render"/> calls.
	/// </summary>
	public void RegisterTypes(TransformResult result)
	{
		foreach (var obj in result.Objects)
			_globalObjects.TryAdd(obj.ClassName, obj);
		foreach (var union in result.TaggedUnions)
		{
			_globalUnions.TryAdd(union.ClassName, union);
			_globalObjects.TryAdd(union.ClassName, new ObjectShape
			{
				ClassName = union.ClassName,
				Namespace = union.Namespace,
				Fields = []
			});
		}
	}

	public void Render(TransformResult result)
	{
		var allObjects = _globalObjects;

		var nsDir = Path.Combine(_outputDir, result.Namespace);

		// Render enums — enums go in "Common/Enums" regardless of source namespace
		foreach (var enumShape in result.Enums)
		{
			if (!_renderedTypes.Add($"Enum:{enumShape.Namespace}:{enumShape.ClassName}"))
				continue;
			var dir = Path.Combine(_outputDir, "Common", "Enums");
			var ctx = TemplateHelpers.BuildEnumContext(enumShape);
			RenderToFile(_templates.Load("Enum.sbn"), ctx, dir, $"{enumShape.ClassName}.cs");
		}

		// Render object types — place in the namespace determined by ref path
		foreach (var objectShape in result.Objects)
		{
			if (!_renderedTypes.Add($"Object:{objectShape.Namespace}:{objectShape.ClassName}"))
				continue;
			var dir = Path.Combine(_outputDir, objectShape.Namespace, "Types");
			var ctx = TemplateHelpers.BuildObjectContext(objectShape, allObjects);
			RenderToFile(_templates.Load("Response.sbn"), ctx, dir, $"{objectShape.ClassName}.cs");
		}

		// Render requests + responses
		foreach (var request in result.Requests)
		{
			// Request + Endpoint
			var reqDir = Path.Combine(nsDir, "Requests");
			var reqCtx = TemplateHelpers.BuildRequestContext(request, allObjects);
			RenderToFile(_templates.Load("Request.sbn"), reqCtx, reqDir, $"{request.ClassName}.cs");

			// Response
			var respDir = Path.Combine(nsDir, "Responses");
			if (request.Response.IsDictionary)
			{
				var respCtx = TemplateHelpers.BuildDictionaryResponseContext(request.Response, allObjects);
				RenderToFile(_templates.Load("DictionaryResponse.sbn"), respCtx, respDir, $"{request.Response.ClassName}.cs");
			}
			else
			{
				var respCtx = TemplateHelpers.BuildResponseContext(request.Response, allObjects);
				RenderToFile(_templates.Load("Response.sbn"), respCtx, respDir, $"{request.Response.ClassName}.cs");
			}
		}

		// Render tagged unions
		foreach (var union in result.TaggedUnions)
		{
			if (!_renderedTypes.Add($"Union:{union.Namespace}:{union.ClassName}"))
				continue;
			var dir = Path.Combine(_outputDir, union.Namespace, "Types");
			var ctx = TemplateHelpers.BuildTaggedUnionContext(union, allObjects);
			RenderToFile(_templates.Load("TaggedUnion.sbn"), ctx, dir, $"{union.ClassName}.cs");
		}

		// Render object descriptors
		foreach (var objectShape in result.Objects)
		{
			if (!_renderedTypes.Add($"ObjectDesc:{objectShape.Namespace}:{objectShape.ClassName}"))
				continue;
			var dir = Path.Combine(_outputDir, objectShape.Namespace, "Descriptors");
			var ctx = TemplateHelpers.BuildObjectDescriptorContext(objectShape, allObjects, _globalUnions);
			RenderToFile(_templates.Load("ObjectDescriptor.sbn"), ctx, dir, $"{objectShape.ClassName}Descriptor.cs");
		}

		// Render tagged union descriptors (skip unions with no variants — empty descriptors cause CS0649)
		foreach (var union in result.TaggedUnions)
		{
			if (union.Variants.Count == 0)
				continue;
			if (!_renderedTypes.Add($"UnionDesc:{union.Namespace}:{union.ClassName}"))
				continue;
			var dir = Path.Combine(_outputDir, union.Namespace, "Descriptors");
			var ctx = TemplateHelpers.BuildTaggedUnionDescriptorContext(union, allObjects, _globalUnions);
			RenderToFile(_templates.Load("TaggedUnionDescriptor.sbn"), ctx, dir, $"{union.ClassName}Descriptor.cs");
		}

		// Render request descriptors
		foreach (var request in result.Requests)
		{
			var dir = Path.Combine(nsDir, "Descriptors");
			var ctx = TemplateHelpers.BuildRequestDescriptorContext(request, allObjects, _globalUnions);
			RenderToFile(_templates.Load("RequestDescriptor.sbn"), ctx, dir, $"{request.ClassName}Descriptor.cs");
		}

		// Render namespace client
		var nsClientCtx = TemplateHelpers.BuildNamespaceClientContext(result.Namespace, result.Requests, allObjects, _globalUnions);
		RenderToFile(_templates.Load("NamespaceClient.sbn"), nsClientCtx, _outputDir, $"{result.Namespace}Namespace.cs");

		// Render client extension (partial class)
		var clientExtCtx = TemplateHelpers.BuildClientExtensionContext(result.Namespace);
		RenderToFile(_templates.Load("ClientExtension.sbn"), clientExtCtx, _outputDir, $"OpenSearchClient.{result.Namespace}.cs");
	}

	private static void RenderToFile(Template template, ScriptObject context, string directory, string fileName)
	{
		Directory.CreateDirectory(directory);

		var templateContext = new TemplateContext
		{
			StrictVariables = true
		};
		templateContext.PushGlobal(context);

		var output = template.Render(templateContext);

		if (template.HasErrors)
			throw new InvalidOperationException(
				$"Template errors rendering {fileName}: {string.Join("; ", template.Messages.Select(m => m.Message))}");

		var filePath = Path.Combine(directory, fileName);
		File.WriteAllText(filePath, output);
	}
}
