namespace Cuteribs.CopilotPrReviewer.Configuration;

public sealed class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public string? AuthType { get; init; }
    public string BaseUrl { get; init; } = "https://dev.azure.com";
}

public sealed class CopilotSettings
{
    public const string SectionName = "Copilot";

    public string Model { get; init; } = "gpt-5-mini";
    public int MaxTokensPerBatch { get; init; } = 90_000;
    public int OverheadTokens { get; init; } = 20_000;
    public int MaxParallelBatches { get; init; } = 4;
    public int TimeoutSeconds { get; init; } = 300;
}

public sealed class ReviewSettings
{
    public const string SectionName = "Review";

    public string? GuidelinesPath { get; init; }
    public bool PostComments { get; init; } = true;
}
