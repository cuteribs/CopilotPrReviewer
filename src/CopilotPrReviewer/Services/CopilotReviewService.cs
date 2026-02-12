using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Models;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class CopilotReviewService
{
	private readonly CopilotSettings _settings;
	private readonly GuidelineProvider _guidelineProvider;
	private readonly ILogger<CopilotReviewService> _logger;

	public CopilotReviewService(
		IOptions<CopilotSettings> settings,
		GuidelineProvider guidelineProvider,
		ILogger<CopilotReviewService> logger
	)
	{
		_settings = settings.Value;
		_guidelineProvider = guidelineProvider;
		_logger = logger;
	}

	public async Task<List<ReviewFinding>> ReviewBatchAsync(ReviewBatch batch)
	{
		var prompt = BuildReviewPrompt(batch);

		await using var client = new CopilotClient(new() { GithubToken = GetGithubToken() });
		await client.StartAsync();

		await using var session = await client.CreateSessionAsync(new SessionConfig
		{
			Model = _settings.Model
		});

		var responseBuilder = new StringBuilder();
		var done = new TaskCompletionSource();

		session.On(ev =>
		{
			if (ev is AssistantMessageEvent msg)
			{
				responseBuilder.Append(msg.Data.Content);
			}
			else if (ev is SessionIdleEvent)
			{
				done.TrySetResult();
			}
			else if (ev is ToolExecutionStartEvent execStart)
			{
				_logger.LogWarning(execStart.Data.ToolName);
			}
			else if (ev is SessionErrorEvent err)
			{
				throw new InvalidOperationException(err.Data.Message);
			}

			_logger.LogInformation("==== {EventType} ====", ev.GetType().Name);
		});

		await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, TimeSpan.FromSeconds(_settings.TimeoutSeconds));
		await done.Task;

		var response = responseBuilder.ToString();
		_logger.LogDebug("Batch {BatchNumber} response length: {Length}", batch.BatchNumber, response.Length);

		var parser = new FindingParser(_logger);
		return parser.Parse(response);
	}

	private string BuildReviewPrompt(ReviewBatch batch)
	{
		var sb = new StringBuilder();

		sb.AppendLine("You are an expert code reviewer. Review the following code changes and report issues.");
		sb.AppendLine();

		// Add guidelines for the tech stack
		var guidelines = _guidelineProvider.GetGuidelines(batch.TechStack);

		if (!string.IsNullOrEmpty(guidelines))
		{
			sb.AppendLine("## Review Guidelines");
			sb.AppendLine(guidelines);
			sb.AppendLine();
		}

		// Add files to review
		sb.AppendLine("## Files to Review");
		sb.AppendLine();

		foreach (var (filePath, file) in batch.Files)
		{
			sb.AppendLine($"### File: {filePath}");
			sb.AppendLine();

			if (!string.IsNullOrEmpty(file.Content))
			{
				sb.AppendLine("#### Full Content:");
				sb.AppendLine("```");
				sb.AppendLine(file.Content);
				sb.AppendLine("```");
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(file.Diff))
			{
				sb.AppendLine("#### Diff:");
				sb.AppendLine("```diff");
				sb.AppendLine(file.Diff);
				sb.AppendLine("```");
				sb.AppendLine();
			}
		}

		// Output format instruction (converted to a single raw string literal)
		var outputFormat = GuidelineProvider.GetReviewOutputFormat();
		sb.AppendLine(outputFormat);

		return sb.ToString();
	}

	private static string? GetGithubToken()
	{
		var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		return token;
	}
}
