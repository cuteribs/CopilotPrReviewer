using Cuteribs.CopilotPrReviewer.Configuration;
using Cuteribs.CopilotPrReviewer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;

var prUrlArg = new Argument<string>("--pr-url") { Description = "The URL of the pull request to review" };
var patOption = new Option<string?>("--pat") { Description = "Azure DevOps personal access token" };
var modelOption = new Option<string?>("--model") { Description = "AI model to use for review" };
var guidelinesPathOption = new Option<string?>("--guidelines-path") { Description = "Path to external guidelines folder" };
var maxParallelOption = new Option<int?>("--max-parallel") { Description = "Maximum parallel batch reviews" };
var noCommentsOption = new Option<bool>("--no-comments") { Description = "Skip posting comments to PR", DefaultValueFactory = _ => false };

var rootCommand = new RootCommand("CopilotPrReviewer - AI-powered PR code reviewer for Azure DevOps")
{
	prUrlArg,
	patOption,
	modelOption,
	guidelinesPathOption,
	maxParallelOption,
	noCommentsOption
};

rootCommand.SetAction(async parseResult =>
{
	var prUrl = parseResult.GetRequiredValue(prUrlArg)!;
	var pat = parseResult.GetValue(patOption);
	var model = parseResult.GetValue(modelOption);
	var guidelinesPath = parseResult.GetValue(guidelinesPathOption);
	var maxParallel = parseResult.GetValue(maxParallelOption);
	var noComments = parseResult.GetValue(noCommentsOption);

	// Build configuration: appsettings.json < env vars < CLI args
	var configBuilder = new ConfigurationBuilder()
		.SetBasePath(AppContext.BaseDirectory)
		.AddJsonFile("appsettings.json", optional: true)
		.AddEnvironmentVariables();

	// Override with CLI args
	var cliOverrides = new Dictionary<string, string?>();

	if (!string.IsNullOrEmpty(pat))
		cliOverrides[$"{AzureDevOpsSettings.SectionName}:Pat"] = pat;

	if (!string.IsNullOrEmpty(model))
		cliOverrides[$"{CopilotSettings.SectionName}:Model"] = model;

	if (!string.IsNullOrEmpty(guidelinesPath))
		cliOverrides[$"{ReviewSettings.SectionName}:GuidelinesPath"] = guidelinesPath;

	if (maxParallel.HasValue)
		cliOverrides[$"{CopilotSettings.SectionName}:MaxParallelBatches"] = maxParallel.Value.ToString();

	if (noComments)
		cliOverrides[$"{ReviewSettings.SectionName}:PostComments"] = "false";

	if (cliOverrides.Count > 0)
		configBuilder.AddInMemoryCollection(cliOverrides);

	var configuration = configBuilder.Build();

	// Build DI container
	var services = new ServiceCollection();

	services.AddLogging(builder => builder
		.AddConsole()
		.SetMinimumLevel(LogLevel.Information));

	services.Configure<AzureDevOpsSettings>(configuration.GetSection(AzureDevOpsSettings.SectionName));
	services.Configure<CopilotSettings>(configuration.GetSection(CopilotSettings.SectionName));
	services.Configure<ReviewSettings>(configuration.GetSection(ReviewSettings.SectionName));

	services.AddHttpClient<AzureDevOpsClient>();

	services.AddSingleton<TechStackClassifier>();
	services.AddSingleton<FileExclusionFilter>();
	services.AddSingleton<TokenCounter>();
	services.AddSingleton<BatchBuilder>();
	services.AddSingleton<GuidelineProvider>();
	services.AddSingleton<FindingParser>();
	services.AddSingleton<CopilotReviewService>();
	services.AddSingleton<ConsoleReporter>();
	services.AddSingleton<ReviewOrchestrator>();

	await using var provider = services.BuildServiceProvider();

	var orchestrator = provider.GetRequiredService<ReviewOrchestrator>();
	var reporter = provider.GetRequiredService<ConsoleReporter>();

	try
	{
		var summary = await orchestrator.RunAsync(prUrl);
		reporter.PrintSummary(summary);
	}
	catch (Exception ex)
	{
		var logger = provider.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Review failed");
		Environment.ExitCode = 1;
	}
});

return await rootCommand.Parse(args).InvokeAsync();
