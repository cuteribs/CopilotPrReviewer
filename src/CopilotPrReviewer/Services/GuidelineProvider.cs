using CopilotPrReviewer.Configuration;
using CopilotPrReviewer.Models;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CopilotPrReviewer.Services;

public sealed class GuidelineProvider
{
	private readonly ReviewSettings _settings;
	private readonly Dictionary<TechStack, string> _cache = new();

	private static readonly Dictionary<TechStack, string> ResourceNames = new()
	{
		[TechStack.Dotnet] = "CopilotPrReviewer.Resources.dotnet-guidelines.md",
		[TechStack.Frontend] = "CopilotPrReviewer.Resources.frontend-guidelines.md",
		[TechStack.Python] = "CopilotPrReviewer.Resources.python-guidelines.md",
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

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream == null) return null;

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
