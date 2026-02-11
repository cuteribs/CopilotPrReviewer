namespace Cuteribs.CopilotPrReviewer.Configuration;

public sealed class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public string? AuthType { get; init; }
    public string BaseUrl { get; init; } = "https://dev.azure.com";
}

