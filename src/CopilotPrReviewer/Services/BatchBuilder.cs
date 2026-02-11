using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Models;
using Microsoft.Extensions.Options;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class BatchBuilder
{
    private readonly TechStackClassifier _classifier;
    private readonly FileExclusionFilter _exclusionFilter;
    private readonly TokenCounter _tokenCounter;
    private readonly CopilotSettings _settings;

    public BatchBuilder(
        TechStackClassifier classifier,
        FileExclusionFilter exclusionFilter,
        TokenCounter tokenCounter,
        IOptions<CopilotSettings> settings)
    {
        _classifier = classifier;
        _exclusionFilter = exclusionFilter;
        _tokenCounter = tokenCounter;
        _settings = settings.Value;
    }

    public BatchResult CreateBatches(FilePatch[] filePatches)
    {
        var availableTokens = _settings.MaxTokensPerBatch - _settings.OverheadTokens;
        var excluded = new List<string>();
        var stacks = new Dictionary<TechStack, List<FilePatch>>();

        foreach (var file in filePatches)
        {
            var stack = _classifier.Classify(file.FilePath);
            if (stack == null)
            {
                excluded.Add(file.FilePath);
                continue;
            }

            if (_exclusionFilter.ShouldExclude(file.FilePath))
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
                var fileTokens = _tokenCounter.CountTokens(content) + _tokenCounter.CountTokens(diff);

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

public sealed class BatchResult
{
    public required List<ReviewBatch> Batches { get; init; }
    public required List<string> ExcludedFiles { get; init; }
}
