using CopilotPrReviewer.Configuration;
using CopilotPrReviewer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace CopilotPrReviewer.Services;

public sealed class ReviewOrchestrator
{
	private readonly AzureDevOpsClient _adoClient;
	private readonly BatchBuilder _batchBuilder;
	private readonly CopilotReviewService _copilotService;
	private readonly CopilotSettings _copilotSettings;
	private readonly ReviewSettings _reviewSettings;
	private readonly ILogger<ReviewOrchestrator> _logger;

	public ReviewOrchestrator(
		AzureDevOpsClient adoClient,
		BatchBuilder batchBuilder,
		CopilotReviewService copilotService,
		IOptions<CopilotSettings> copilotSettings,
		IOptions<ReviewSettings> reviewSettings,
		ILogger<ReviewOrchestrator> logger
	)
	{
		_adoClient = adoClient;
		_batchBuilder = batchBuilder;
		_copilotService = copilotService;
		_copilotSettings = copilotSettings.Value;
		_reviewSettings = reviewSettings.Value;
		_logger = logger;
	}

	public async Task<ReviewSummary> RunAsync(string prUrl)
	{
		var stopwatch = Stopwatch.StartNew();

		// 1. Parse URL
		var prInfo = _adoClient.ParsePrUrl(prUrl);
		_logger.LogInformation("Reviewing PR #{PrId} in {Org}/{Project}/{Repo}",
			prInfo.PullRequestId, prInfo.Organization, prInfo.Project, prInfo.Repository);

		// 2. Fetch PR metadata
		var prDetails = await _adoClient.GetPrDetailsAsync(prInfo);
		_logger.LogInformation("PR: {Title} ({Source} → {Target})",
			prDetails.Title,
			prDetails.SourceRefName.Replace("refs/heads/", ""),
			prDetails.TargetRefName.Replace("refs/heads/", ""));

		// 3. Fetch file changes
		var filePatches = await _adoClient.FetchPrChangesAsync(prInfo, prDetails);
		_logger.LogInformation("Fetched {Count} file changes", filePatches.Length);

		// 4. Classify + filter + batch
		var batchResult = _batchBuilder.CreateBatches(filePatches);
		_logger.LogInformation("Created {BatchCount} batches, excluded {ExcludedCount} files",
            batchResult.Batches.Count, batchResult.ExcludedFiles.Count);

		// 5. Review batches in parallel
		var allFindings = new List<ReviewFinding>();
		var semaphore = new SemaphoreSlim(_copilotSettings.MaxParallelBatches);

		var tasks = batchResult.Batches.Select(async batch =>
		{
			await semaphore.WaitAsync();
			try
			{
				_logger.LogInformation("Reviewing batch {BatchNumber} ({TechStack}, {FileCount} files, ~{Tokens} tokens)",
					batch.BatchNumber, batch.TechStack, batch.FileCount, batch.TotalTokens);

				var findings = await _copilotService.ReviewBatchAsync(batch);

				_logger.LogInformation("Batch {BatchNumber} completed: {FindingCount} findings",
					batch.BatchNumber, findings.Count);

				return findings;
			}
			finally
			{
				semaphore.Release();
			}
		});

		var results = await Task.WhenAll(tasks);
		foreach (var findings in results)
			allFindings.AddRange(findings);

		// 6. Post comments to ADO
		bool commentsPosted = false;
		if (_reviewSettings.PostComments && allFindings.Count > 0)
		{
			_logger.LogInformation("Posting {Count} comments to PR...", allFindings.Count);
			await PostFindingsAsync(prInfo, allFindings);
			commentsPosted = true;
		}

		stopwatch.Stop();

		return new ReviewSummary
		{
			PrUrl = prUrl,
			PrTitle = prDetails.Title ?? "Untitled",
			TotalFiles = filePatches.Length,
			ReviewedFiles = batchResult.Batches.Sum(b => b.FileCount),
			ExcludedFiles = batchResult.ExcludedFiles.Count,
			TotalBatches = batchResult.Batches.Count,
			Findings = allFindings,
			CommentsPosted = commentsPosted,
			Duration = stopwatch.Elapsed
		};
	}

	private async Task PostFindingsAsync(PrInfo prInfo, List<ReviewFinding> findings)
	{
		foreach (var finding in findings)
		{
			try
			{
				var commentText = finding.Suggestion != null
					? $"{finding.Description}\n\n```suggestion\n{finding.Suggestion}\n```"
					: finding.Description;

				await _adoClient.PostPrCommentAsync(prInfo, new PrCommentOptions
				{
					CommentText = commentText,
					FilePath = finding.FilePath,
					LineNumber = finding.LineNumber > 0 ? finding.LineNumber : 1,
					Severity = finding.Severity
				});
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to post comment for {FilePath}:{Line}",
					finding.FilePath, finding.LineNumber);
			}
		}
	}
}
