namespace Cuteribs.CopilotPrReviewer.Models;

public sealed class ReviewFinding
{
    public required string FilePath { get; init; }
    public int LineNumber { get; init; }
    public required string Severity { get; init; }
    public required string Description { get; init; }
    public string? Suggestion { get; init; }
}
