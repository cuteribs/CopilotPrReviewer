using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Models;
using Microsoft.Extensions.Options;
using SharpToken;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class BatchBuilder
{
    private readonly CopilotSettings _settings;
    private readonly GptEncoding _encoding = GptEncoding.GetEncoding("cl100k_base");

    private static readonly Dictionary<string, TechStack> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = TechStack.Dotnet, [".csproj"] = TechStack.Dotnet, [".sln"] = TechStack.Dotnet,
        [".slnx"] = TechStack.Dotnet, [".props"] = TechStack.Dotnet, [".razor"] = TechStack.Dotnet,
        [".cshtml"] = TechStack.Dotnet,
        [".js"] = TechStack.Frontend, [".jsx"] = TechStack.Frontend, [".ts"] = TechStack.Frontend,
        [".tsx"] = TechStack.Frontend, [".html"] = TechStack.Frontend, [".htm"] = TechStack.Frontend,
        [".css"] = TechStack.Frontend, [".scss"] = TechStack.Frontend, [".sass"] = TechStack.Frontend,
        [".less"] = TechStack.Frontend, [".vue"] = TechStack.Frontend, [".svelte"] = TechStack.Frontend,
        [".py"] = TechStack.Python, [".pyi"] = TechStack.Python, [".pyx"] = TechStack.Python,
        [".pxd"] = TechStack.Python,
        [".json"] = TechStack.Config, [".yaml"] = TechStack.Config, [".yml"] = TechStack.Config,
        [".http"] = TechStack.Config, [".rest"] = TechStack.Config,
    };

    private static readonly string[] ExactNameExclusions =
    [
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
        "pipfile.lock", "poetry.lock", "packages.lock.json",
    ];

    private static readonly string[] SuffixExclusions =
    [
        ".min.js", ".min.css", ".d.ts", ".g.cs", ".Designer.cs", ".generated.cs",
    ];

    public BatchBuilder(IOptions<CopilotSettings> settings)
    {
        _settings = settings.Value;
    }

    internal static TechStack? ClassifyFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out var stack) ? stack : null;
    }

    internal static bool ShouldExclude(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        foreach (var name in ExactNameExclusions)
            if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;

        foreach (var suffix in SuffixExclusions)
            if (filePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    internal int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return _encoding.Encode(text).Count;
    }

    public BatchResult CreateBatches(FilePatch[] filePatches)
    {
        var availableTokens = _settings.MaxTokensPerBatch - _settings.OverheadTokens;
        var excluded = new List<string>();
        var stacks = new Dictionary<TechStack, List<FilePatch>>();

        foreach (var file in filePatches)
        {
            var stack = ClassifyFile(file.FilePath);
            if (stack == null)
            {
                excluded.Add(file.FilePath);
                continue;
            }

            if (ShouldExclude(file.FilePath))
            {
                excluded.Add(file.FilePath);
                continue;
            }

            if (!stacks.ContainsKey(stack.Value))
                stacks[stack.Value] = [];

            stacks[stack.Value].Add(file);
        }

        // Sort files within each stack by size (ascending) for better packing
        foreach (var stack in stacks.Keys)
            stacks[stack].Sort((a, b) =>
                ((a.NewContent?.Length ?? 0) + a.Patch.Length)
                .CompareTo((b.NewContent?.Length ?? 0) + b.Patch.Length));

        var batches = new List<ReviewBatch>();
        int batchNumber = 1;

        foreach (var (stack, files) in stacks)
        {
            var currentFiles = new Dictionary<string, BatchFile>();
            int currentTokens = 0;

            foreach (var file in files)
            {
                var content = file.NewContent ?? "";
                var diff = file.Patch;
                var fileTokens = CountTokens(content) + CountTokens(diff);

                if (currentTokens + fileTokens > availableTokens && currentFiles.Count > 0)
                {
                    batches.Add(new ReviewBatch
                    {
                        BatchNumber = batchNumber++,
                        TechStack = stack,
                        TotalTokens = currentTokens,
                        Files = currentFiles
                    });
                    currentFiles = new Dictionary<string, BatchFile>();
                    currentTokens = 0;
                }

                currentFiles[file.FilePath] = new BatchFile
                {
                    Content = content,
                    Diff = diff
                };
                currentTokens += fileTokens;
            }

            if (currentFiles.Count > 0)
            {
                batches.Add(new ReviewBatch
                {
                    BatchNumber = batchNumber++,
                    TechStack = stack,
                    TotalTokens = currentTokens,
                    Files = currentFiles
                });
            }
        }

        return new BatchResult
        {
            Batches = batches,
            ExcludedFiles = excluded
        };
    }
}
