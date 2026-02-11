using CopilotPrReviewer.Configuration;
using CopilotPrReviewer.Models;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CopilotPrReviewer.Services;

public partial class AzureDevOpsClient
{
    private const string ApiVersion = "7.1";
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsSettings _settings;

    public AzureDevOpsClient(HttpClient httpClient, IOptions<AzureDevOpsSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public PrInfo ParsePrUrl(string prUrl)
    {
        var prInfo = ParsePrUrlInternal(prUrl, DevAzureRegex())
                     ?? ParsePrUrlInternal(prUrl, VisualStudioRegex());

        if (prInfo == null)
            throw new ArgumentException("Invalid Azure DevOps PR URL format", nameof(prUrl));

        return prInfo;
    }

    public async Task<PullRequest> GetPrDetailsAsync(PrInfo prInfo)
    {
        var baseUrl = GetBaseUrl(prInfo);
        var url = $"{baseUrl}/pullRequests/{prInfo.PullRequestId}?api-version={ApiVersion}";

        var prDetails = await SendRequestAsync<PullRequest>(url, HttpMethod.Get, null, "Failed to get PR details");

        if (!prDetails.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The PR is not active.");

        if (!prDetails.MergeStatus.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The PR has merge conflict.");

        return prDetails;
    }

    public async Task<FilePatch[]> FetchPrChangesAsync(PrInfo prInfo, PullRequest prDetails)
    {
        var baseUrl = GetBaseUrl(prInfo);

        var sourceBranch = Uri.EscapeDataString(prDetails.SourceRefName.Replace("refs/heads/", ""));
        var targetBranch = Uri.EscapeDataString(prDetails.TargetRefName.Replace("refs/heads/", ""));

        if (string.IsNullOrEmpty(sourceBranch) || string.IsNullOrEmpty(targetBranch))
            throw new InvalidOperationException("Could not determine source or target branch from PR details.");

        var diffsUrl = $"{baseUrl}/diffs/commits?baseVersion={targetBranch}&targetVersion={sourceBranch}&$top=2000&api-version={ApiVersion}";
        var data = await SendRequestAsync<CommitDiffs>(diffsUrl, HttpMethod.Get, null, "Failed to get git changes");
        var changes = data.Changes ?? [];

        if (changes.Length == 0)
            throw new InvalidOperationException("No changed files found in this PR.");

        var fileItems = changes.Where(IsSupportedChange).ToArray();

        if (fileItems.Length == 0)
            throw new InvalidOperationException("No supported code file found in this PR.");

        var getFileTasks = fileItems.Select(c => GetFilePatchAsync(c, baseUrl));
        return await Task.WhenAll(getFileTasks);
    }

    public async Task PostPrCommentAsync(PrInfo prInfo, PrCommentOptions options)
    {
        var baseUrl = GetBaseUrl(prInfo);
        var threadUrl = $"{baseUrl}/pullRequests/{prInfo.PullRequestId}/threads?api-version={ApiVersion}";

        var commentContent = FormatCommentWithSeverity(options.CommentText, options.Severity);

        var thread = new PrThread
        {
            Comments = [new PrComment { Content = commentContent }],
            ThreadContext = new ThreadContext
            {
                FilePath = options.FilePath,
                RightFileStart = new FilePosition { Line = options.LineNumber, Offset = 1 },
                RightFileEnd = new FilePosition { Line = options.LineNumber, Offset = 999 }
            }
        };

        await SendRequestAsync(threadUrl, HttpMethod.Post, thread, "Failed to create thread");
    }

    #region Private Helpers

    private string GetBaseUrl(PrInfo prInfo) =>
        $"{_settings.BaseUrl}/{prInfo.Organization}/{prInfo.Project}/_apis/git/repositories/{prInfo.Repository}";

    [GeneratedRegex(@"https://dev\.azure\.com/(.+?)/(.+?)/_git/(.+?)/pullrequest/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DevAzureRegex();

    [GeneratedRegex(@"https://(.+?)\.visualstudio\.com/(.+?)/_git/(.+?)/pullrequest/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex VisualStudioRegex();

    private static PrInfo? ParsePrUrlInternal(string prUrl, Regex pattern)
    {
        var match = pattern.Match(prUrl);
        if (!match.Success) return null;

        return new PrInfo
        {
            Organization = match.Groups[1].Value,
            Project = match.Groups[2].Value,
            Repository = match.Groups[3].Value,
            PullRequestId = int.Parse(match.Groups[4].Value)
        };
    }

    private static bool IsSupportedChange(GitChange change)
    {
        string[] supportedChangeTypes = ["add", "edit", "delete", "rename"];
        return supportedChangeTypes.Contains(change.ChangeType, StringComparer.OrdinalIgnoreCase)
               && change.Item.GitObjectType.Equals("blob", StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrEmpty(change.Item.Path)
               && !string.IsNullOrEmpty(change.Item.Url);
    }

    private static string FormatCommentWithSeverity(string commentText, string? severity)
    {
        if (string.IsNullOrEmpty(severity))
            return commentText;

        if (commentText.TrimStart().StartsWith("**["))
            return commentText;

        return $"**[{severity}]**\n\n{commentText}";
    }

    private void ConfigureAuthHeaders(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(_settings.Pat))
            throw new InvalidOperationException("Azure DevOps PAT is not configured.");

        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_settings.Pat}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    private async Task<T> SendRequestAsync<T>(string url, HttpMethod method, object? body = null, string errorMessage = "Error")
    {
        using var request = new HttpRequestMessage(method, url);
        ConfigureAuthHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var jsonContent = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{errorMessage}: HTTP {response.StatusCode}: {response.ReasonPhrase}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private async Task SendRequestAsync(string url, HttpMethod method, object? body = null, string errorMessage = "Error")
    {
        using var request = new HttpRequestMessage(method, url);
        ConfigureAuthHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var jsonContent = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{errorMessage}: HTTP {response.StatusCode}: {response.ReasonPhrase}");
    }

    private async Task<string?> GetBlobContentAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ConfigureAuthHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<FilePatch> GetFilePatchAsync(GitChange gitChange, string baseUrl)
    {
        var fileItem = gitChange.Item;
        var filePath = fileItem.Path;
        string? sourceContent = null;
        string? newContent = null;

        if (!string.IsNullOrEmpty(fileItem.OriginalObjectId))
        {
            var url = $"{baseUrl}/blobs/{fileItem.OriginalObjectId}?api-version={ApiVersion}";
            sourceContent = await GetBlobContentAsync(url);
        }

        if (!string.IsNullOrEmpty(fileItem.ObjectId))
        {
            var url = $"{baseUrl}/blobs/{fileItem.ObjectId}?api-version={ApiVersion}";
            newContent = await GetBlobContentAsync(url);
        }

        var (patch, linesAdded, linesDeleted) = GenerateUnifiedDiffWithStats(filePath, sourceContent ?? "", newContent ?? "");

        return new FilePatch
        {
            FilePath = filePath,
            SourceContent = sourceContent,
            NewContent = newContent,
            Patch = patch,
            ChangeType = gitChange.ChangeType,
            LinesAdded = linesAdded,
            LinesDeleted = linesDeleted
        };
    }

    internal static (string patch, int linesAdded, int linesDeleted) GenerateUnifiedDiffWithStats(string fileName, string oldText, string newText)
    {
        var differ = new Differ();
        var builder = new InlineDiffBuilder(differ);
        var diff = builder.BuildDiffModel(oldText, newText);

        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{fileName}");
        sb.AppendLine($"+++ b/{fileName}");

        var oldLineNumber = 1;
        var newLineNumber = 1;
        var hunkStart = 0;
        var hunkLines = new List<string>();
        int linesAdded = 0;
        int linesDeleted = 0;

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Unchanged:
                    if (hunkLines.Count > 0)
                    {
                        WriteHunk(sb, hunkStart, oldLineNumber - hunkStart, newLineNumber - hunkStart, hunkLines);
                        hunkLines.Clear();
                    }
                    hunkStart = oldLineNumber;
                    oldLineNumber++;
                    newLineNumber++;
                    break;

                case ChangeType.Deleted:
                    if (hunkLines.Count == 0) hunkStart = oldLineNumber;
                    hunkLines.Add($"-{line.Text}");
                    linesDeleted++;
                    oldLineNumber++;
                    break;

                case ChangeType.Inserted:
                    if (hunkLines.Count == 0) hunkStart = newLineNumber;
                    hunkLines.Add($"+{line.Text}");
                    linesAdded++;
                    newLineNumber++;
                    break;

                case ChangeType.Modified:
                    if (hunkLines.Count == 0) hunkStart = oldLineNumber;
                    hunkLines.Add($"-{line.Text}");
                    hunkLines.Add($"+{line.Text}");
                    linesDeleted++;
                    linesAdded++;
                    oldLineNumber++;
                    newLineNumber++;
                    break;
            }
        }

        if (hunkLines.Count > 0)
            WriteHunk(sb, hunkStart, oldLineNumber - hunkStart, newLineNumber - hunkStart, hunkLines);

        return (sb.ToString(), linesAdded, linesDeleted);
    }

    private static void WriteHunk(StringBuilder sb, int oldStart, int oldCount, int newCount, List<string> lines)
    {
        sb.AppendLine($"@@ -{oldStart},{oldCount} +{oldStart},{newCount} @@");
        foreach (var line in lines)
            sb.AppendLine(line);
    }

    #endregion
}
