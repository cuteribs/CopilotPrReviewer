using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Models;
using Cuteribs.CopilotPrReviewer.Services;
using Microsoft.Extensions.Options;

namespace Cuteribs.CopilotPrReviewer.Tests;

public class BatchBuilderTests
{
    private static BatchBuilder CreateBuilder(int maxTokens = 90_000, int overhead = 20_000)
    {
        var classifier = new TechStackClassifier();
        var filter = new FileExclusionFilter();
        var tokenCounter = new TokenCounter();
        var settings = Options.Create(new CopilotSettings
        {
            MaxTokensPerBatch = maxTokens,
            OverheadTokens = overhead
        });
        return new BatchBuilder(classifier, filter, tokenCounter, settings);
    }

    private static FilePatch MakePatch(string path, string content = "test", string changeType = "edit")
    {
        return new FilePatch
        {
            FilePath = path,
            NewContent = content,
            Patch = $"--- a/{path}\n+++ b/{path}\n@@ -1,1 +1,1 @@\n+{content}",
            ChangeType = changeType,
            LinesAdded = 1,
            LinesDeleted = 0
        };
    }

    [Fact]
    public void CreateBatches_SingleDotnetFile_OneBatch()
    {
        var builder = CreateBuilder();
        var patches = new[] { MakePatch("/src/Service.cs") };

        var result = builder.CreateBatches(patches);

        Assert.Single(result.Batches);
        Assert.Equal(TechStack.Dotnet, result.Batches[0].TechStack);
        Assert.Single(result.Batches[0].Files);
        Assert.Empty(result.ExcludedFiles);
    }

    [Fact]
    public void CreateBatches_MixedTechStacks_GroupsByStack()
    {
        var builder = CreateBuilder();
        var patches = new[]
        {
            MakePatch("/src/Service.cs"),
            MakePatch("/src/app.tsx"),
            MakePatch("/src/script.py"),
        };

        var result = builder.CreateBatches(patches);

        Assert.Equal(3, result.Batches.Count);
        Assert.Contains(result.Batches, b => b.TechStack == TechStack.Dotnet);
        Assert.Contains(result.Batches, b => b.TechStack == TechStack.Frontend);
        Assert.Contains(result.Batches, b => b.TechStack == TechStack.Python);
    }

    [Fact]
    public void CreateBatches_ExcludesUnknownExtensions()
    {
        var builder = CreateBuilder();
        var patches = new[]
        {
            MakePatch("/src/Service.cs"),
            MakePatch("/docs/readme.md"),
            MakePatch("/assets/logo.png"),
        };

        var result = builder.CreateBatches(patches);

        Assert.Single(result.Batches);
        Assert.Equal(2, result.ExcludedFiles.Count);
    }

    [Fact]
    public void CreateBatches_ExcludesLockFiles()
    {
        var builder = CreateBuilder();
        var patches = new[]
        {
            MakePatch("/src/app.js"),
            MakePatch("/package-lock.json"),
        };

        var result = builder.CreateBatches(patches);

        Assert.Single(result.Batches);
        Assert.Single(result.ExcludedFiles);
        Assert.Contains("/package-lock.json", result.ExcludedFiles);
    }

    [Fact]
    public void CreateBatches_LargeContent_SplitsIntoBatches()
    {
        // Use very small token budget to force splitting
        var builder = CreateBuilder(maxTokens: 100, overhead: 0);
        var largeContent = new string('x', 500); // ~125 tokens

        var patches = new[]
        {
            MakePatch("/src/File1.cs", largeContent),
            MakePatch("/src/File2.cs", largeContent),
        };

        var result = builder.CreateBatches(patches);

        Assert.True(result.Batches.Count >= 2, "Should have split into multiple batches");
    }
}
