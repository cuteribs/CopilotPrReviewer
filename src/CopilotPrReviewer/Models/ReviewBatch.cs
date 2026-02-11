namespace Cuteribs.CopilotPrReviewer.Models;

public sealed class ReviewBatch
{
    public required int BatchNumber { get; init; }
    public required TechStack TechStack { get; init; }
    public required int TotalTokens { get; init; }
    public required Dictionary<string, BatchFile> Files { get; init; }

    public int FileCount => Files.Count;
}

public sealed class BatchFile
{
    public required string Content { get; init; }
    public required string Diff { get; init; }
}
