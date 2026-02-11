namespace Cuteribs.CopilotPrReviewer.Models;

public record PullRequest
{
    public required int PullRequestId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string MergeStatus { get; init; }
    public required string SourceRefName { get; init; }
    public required string TargetRefName { get; init; }
    public string? CreationDate { get; init; }
    public PullRequestCreator? CreatedBy { get; init; }
}

public record PullRequestCreator
{
    public string? DisplayName { get; init; }
    public string? UniqueName { get; init; }
}
