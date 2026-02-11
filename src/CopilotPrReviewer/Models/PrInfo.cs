namespace Cuteribs.CopilotPrReviewer.Models;

public record PrInfo
{
    public required string Organization { get; init; }
    public required string Project { get; init; }
    public required string Repository { get; init; }
    public required int PullRequestId { get; init; }
}
