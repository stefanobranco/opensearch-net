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

	public CodeRenderer(string outputDir)
	{
		_outputDir = outputDir;
		// Templates are next to the CodeGen project
		var templatesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates");
		templatesDir = Path.GetFullPath(templatesDir);
		_templates = new TemplateLoader(templatesDir);
	}

	public void Render(TransformResult result)
	{
		var nsDir = Path.Combine(_outputDir, result.Namespace);

		// Render enums
		foreach (var enumShape in result.Enums)
		{
			var dir = Path.Combine(_outputDir, "Common", "Enums");
			var ctx = TemplateHelpers.BuildEnumContext(enumShape);
			RenderToFile(_templates.Load("Enum.sbn"), ctx, dir, $"{enumShape.ClassName}.cs");
		}

		// Render object types
		foreach (var objectShape in result.Objects)
		{
			var dir = Path.Combine(nsDir, "Types");
			var ctx = TemplateHelpers.BuildObjectContext(objectShape);
			RenderToFile(_templates.Load("Response.sbn"), ctx, dir, $"{objectShape.ClassName}.cs");
		}

		// Render requests + responses
		foreach (var request in result.Requests)
		{
			// Request + Endpoint
			var reqDir = Path.Combine(nsDir, "Requests");
			var reqCtx = TemplateHelpers.BuildRequestContext(request);
			RenderToFile(_templates.Load("Request.sbn"), reqCtx, reqDir, $"{request.ClassName}.cs");

			// Response
			var respDir = Path.Combine(nsDir, "Responses");
			if (request.Response.IsDictionary)
			{
				var respCtx = TemplateHelpers.BuildDictionaryResponseContext(request.Response);
				RenderToFile(_templates.Load("DictionaryResponse.sbn"), respCtx, respDir, $"{request.Response.ClassName}.cs");
			}
			else
			{
				var respCtx = TemplateHelpers.BuildResponseContext(request.Response);
				RenderToFile(_templates.Load("Response.sbn"), respCtx, respDir, $"{request.Response.ClassName}.cs");
			}
		}

		// Render namespace client
		var nsClientCtx = TemplateHelpers.BuildNamespaceClientContext(result.Namespace, result.Requests);
		RenderToFile(_templates.Load("NamespaceClient.sbn"), nsClientCtx, _outputDir, $"{result.Namespace}Namespace.cs");

		// Render client extension (partial class)
		var clientExtCtx = TemplateHelpers.BuildClientExtensionContext(result.Namespace);
		RenderToFile(_templates.Load("ClientExtension.sbn"), clientExtCtx, _outputDir, $"OpenSearchClient.{result.Namespace}.cs");
	}

	private static void RenderToFile(Template template, ScriptObject context, string directory, string fileName)
	{
		Directory.CreateDirectory(directory);

		var templateContext = new TemplateContext();
		templateContext.PushGlobal(context);

		var output = template.Render(templateContext);
		var filePath = Path.Combine(directory, fileName);
		File.WriteAllText(filePath, output);
	}
}
