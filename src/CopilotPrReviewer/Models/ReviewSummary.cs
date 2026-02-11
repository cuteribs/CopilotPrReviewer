namespace Cuteribs.CopilotPrReviewer.Models;

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
    public int HighCount => Findings.Count(f => f.Severity.Equals("High", StringComparison.OrdinalIgnoreCase));
    public int MediumCount => Findings.Count(f => f.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase));
    public int LowCount => Findings.Count(f => f.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase));
}
