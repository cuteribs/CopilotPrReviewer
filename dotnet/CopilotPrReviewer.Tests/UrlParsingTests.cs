using Cuteribs.CopilotPrReviewer.Services;

namespace Cuteribs.CopilotPrReviewer.Tests;

public class UrlParsingTests
{
    private readonly AzureDevOpsClient _client;

    public UrlParsingTests()
    {
        // Create with dummy settings - only testing URL parsing (static logic)
        _client = CreateClient();
    }

    [Theory]
    [InlineData(
        "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123",
        "myorg", "myproject", "myrepo", 123)]
    [InlineData(
        "https://dev.azure.com/contoso/ProjectX/_git/backend-api/pullrequest/42",
        "contoso", "ProjectX", "backend-api", 42)]
    public void ParsePrUrl_DevAzureFormat_ExtractsComponents(
        string url, string org, string project, string repo, int prId)
    {
        var result = _client.ParsePrUrl(url);

        Assert.Equal(org, result.Organization);
        Assert.Equal(project, result.Project);
        Assert.Equal(repo, result.Repository);
        Assert.Equal(prId, result.PullRequestId);
    }

    [Theory]
    [InlineData(
        "https://myorg.visualstudio.com/myproject/_git/myrepo/pullrequest/456",
        "myorg", "myproject", "myrepo", 456)]
    public void ParsePrUrl_VisualStudioFormat_ExtractsComponents(
        string url, string org, string project, string repo, int prId)
    {
        var result = _client.ParsePrUrl(url);

        Assert.Equal(org, result.Organization);
        Assert.Equal(project, result.Project);
        Assert.Equal(repo, result.Repository);
        Assert.Equal(prId, result.PullRequestId);
    }

    [Theory]
    [InlineData("https://github.com/org/repo/pull/1")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void ParsePrUrl_InvalidFormat_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => _client.ParsePrUrl(url));
    }

    private static AzureDevOpsClient CreateClient()
    {
        var settings = Microsoft.Extensions.Options.Options.Create(
            new Configuration.AzureDevOpsSettings());
        return new AzureDevOpsClient(new HttpClient(), settings);
    }
}
