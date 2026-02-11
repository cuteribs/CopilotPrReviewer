namespace Cuteribs.CopilotPrReviewer.Configuration;

public sealed class ReviewSettings
{
    public const string SectionName = "Review";

    public string? GuidelinesPath { get; init; }
    public bool PostComments { get; init; } = true;
}
