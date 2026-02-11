using CopilotPrReviewer.Models;

namespace CopilotPrReviewer.Services;

public sealed class TechStackClassifier
{
    private static readonly Dictionary<string, TechStack> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET
        [".cs"] = TechStack.Dotnet,
        [".csproj"] = TechStack.Dotnet,
        [".sln"] = TechStack.Dotnet,
        [".slnx"] = TechStack.Dotnet,
        [".props"] = TechStack.Dotnet,
        [".razor"] = TechStack.Dotnet,
        [".cshtml"] = TechStack.Dotnet,

        // Frontend
        [".js"] = TechStack.Frontend,
        [".jsx"] = TechStack.Frontend,
        [".ts"] = TechStack.Frontend,
        [".tsx"] = TechStack.Frontend,
        [".html"] = TechStack.Frontend,
        [".htm"] = TechStack.Frontend,
        [".css"] = TechStack.Frontend,
        [".scss"] = TechStack.Frontend,
        [".sass"] = TechStack.Frontend,
        [".less"] = TechStack.Frontend,
        [".vue"] = TechStack.Frontend,
        [".svelte"] = TechStack.Frontend,

        // Python
        [".py"] = TechStack.Python,
        [".pyi"] = TechStack.Python,
        [".pyx"] = TechStack.Python,
        [".pxd"] = TechStack.Python,

        // Config
        [".json"] = TechStack.Config,
        [".yaml"] = TechStack.Config,
        [".yml"] = TechStack.Config,
        [".http"] = TechStack.Config,
        [".rest"] = TechStack.Config,
    };

    public TechStack? Classify(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out var stack) ? stack : null;
    }
}
