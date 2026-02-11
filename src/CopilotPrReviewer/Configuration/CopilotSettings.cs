namespace CopilotPrReviewer.Configuration;

public sealed class CopilotSettings
{
    public const string SectionName = "Copilot";

    public string Model { get; init; } = "gpt-5-mini";
    public int MaxTokensPerBatch { get; init; } = 90_000;
    public int OverheadTokens { get; init; } = 20_000;
    public int MaxParallelBatches { get; init; } = 4;
}
