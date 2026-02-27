using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Models;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class GuidelineProvider
{
	private readonly ReviewSettings _settings;
	private readonly Dictionary<TechStack, string> _cache = [];

	private static readonly string ReviewOutputFormatName = "Cuteribs.CopilotPrReviewer.Resources.output-format.md";

	private static readonly Dictionary<TechStack, string> ResourceNames = new()
	{
		[TechStack.Dotnet] = "Cuteribs.CopilotPrReviewer.Resources.dotnet-guidelines.md",
		[TechStack.Frontend] = "Cuteribs.CopilotPrReviewer.Resources.frontend-guidelines.md",
		[TechStack.Python] = "Cuteribs.CopilotPrReviewer.Resources.python-guidelines.md",
	};

	private static readonly Dictionary<TechStack, string> FileNames = new()
	{
		[TechStack.Dotnet] = "dotnet-guidelines.md",
		[TechStack.Frontend] = "frontend-guidelines.md",
		[TechStack.Python] = "python-guidelines.md",
	};

	public GuidelineProvider(IOptions<ReviewSettings> settings)
	{
		_settings = settings.Value;
	}

	public string? GetGuidelines(TechStack techStack)
	{
		if (_cache.TryGetValue(techStack, out var cached))
			return cached;

		var content = LoadFromExternalPath(techStack) ?? LoadFromEmbeddedResource(techStack);

		if (content != null)
			_cache[techStack] = content;

		return content;
	}

	public static string GetReviewOutputFormat()
	{
		return LoadFromEmbeddedResource(ReviewOutputFormatName) ?? "!!MISSING OUTPUT FORMAT!!";
	}

	private string? LoadFromExternalPath(TechStack techStack)
	{
		if (string.IsNullOrEmpty(_settings.GuidelinesPath))
			return null;

		if (!FileNames.TryGetValue(techStack, out var fileName))
			return null;

		var filePath = Path.Combine(_settings.GuidelinesPath, fileName);
		return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
	}

	private static string? LoadFromEmbeddedResource(TechStack techStack)
	{
		if (!ResourceNames.TryGetValue(techStack, out var resourceName))
			return null;

		return LoadFromEmbeddedResource(resourceName);
	}

	private static string? LoadFromEmbeddedResource(string resourceName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(resourceName);

		if (stream == null) return null;

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
