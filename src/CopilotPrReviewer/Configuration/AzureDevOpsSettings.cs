namespace Cuteribs.CopilotPrReviewer.Configuration;

public sealed class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public required string Pat { get; init; }
    public string BaseUrl { get; init; } = "https://dev.azure.com";
}
