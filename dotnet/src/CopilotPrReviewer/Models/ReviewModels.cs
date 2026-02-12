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

public sealed class BatchResult
{
    public required List<ReviewBatch> Batches { get; init; }
    public required List<string> ExcludedFiles { get; init; }
}

public sealed class ReviewFinding
{
    public required string FilePath { get; init; }
    public int LineNumber { get; init; }
    public required string Severity { get; init; }
    public required string Description { get; init; }
    public string? Suggestion { get; init; }
}

public sealed class ReviewSummary
{
    public required string PrUrl { get; init; }
    public required string PrTitle { get; init; }
    public required int TotalFiles { get; init; }
    public required int ReviewedFiles { get; init; }
    public required int ExcludedFiles { get; init; }
    public required int TotalBatches { get; init; }
    public required List<ReviewFinding> Findings { get; init; }
    public required bool CommentsPosted { get; init; }
    public required TimeSpan Duration { get; init; }

    public int CriticalCount => Findings.Count(f => f.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));
    public int MajorCount => Findings.Count(f => f.Severity.Equals("Major", StringComparison.OrdinalIgnoreCase));
    public int MinorCount => Findings.Count(f => f.Severity.Equals("Minor", StringComparison.OrdinalIgnoreCase));
}
