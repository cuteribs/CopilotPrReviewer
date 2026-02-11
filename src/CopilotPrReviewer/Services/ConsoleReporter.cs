using CopilotPrReviewer.Models;
using Spectre.Console;

namespace CopilotPrReviewer.Services;

public sealed class ConsoleReporter
{
    public void PrintSummary(ReviewSummary summary)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]Review Summary[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        // Overview table
        var overviewTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Property")
            .AddColumn("Value");

        overviewTable.AddRow("PR URL", summary.PrUrl);
        overviewTable.AddRow("Title", Markup.Escape(summary.PrTitle));
        overviewTable.AddRow("Total Files", summary.TotalFiles.ToString());
        overviewTable.AddRow("Reviewed Files", summary.ReviewedFiles.ToString());
        overviewTable.AddRow("Excluded Files", summary.ExcludedFiles.ToString());
        overviewTable.AddRow("Batches", summary.TotalBatches.ToString());
        overviewTable.AddRow("Total Findings", summary.Findings.Count.ToString());
        overviewTable.AddRow("Comments Posted", summary.CommentsPosted ? "Yes" : "No");
        overviewTable.AddRow("Duration", $"{summary.Duration.TotalSeconds:F1}s");

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Severity breakdown
        if (summary.Findings.Count > 0)
        {
            var severityTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Severity Breakdown[/]")
                .AddColumn("Severity")
                .AddColumn("Count");

            if (summary.CriticalCount > 0)
                severityTable.AddRow("[bold red]Critical[/]", summary.CriticalCount.ToString());
            if (summary.HighCount > 0)
                severityTable.AddRow("[bold orange1]High[/]", summary.HighCount.ToString());
            if (summary.MediumCount > 0)
                severityTable.AddRow("[bold yellow]Medium[/]", summary.MediumCount.ToString());
            if (summary.LowCount > 0)
                severityTable.AddRow("[bold green]Low[/]", summary.LowCount.ToString());

            AnsiConsole.Write(severityTable);
            AnsiConsole.WriteLine();

            // Top findings (Critical + High)
            var topFindings = summary.Findings
                .Where(f => f.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                         || f.Severity.Equals("High", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

            if (topFindings.Count > 0)
            {
                var findingsTable = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Top Critical/High Findings[/]")
                    .AddColumn("Severity")
                    .AddColumn("File")
                    .AddColumn("Line")
                    .AddColumn("Description");

                foreach (var finding in topFindings)
                {
                    var severityColor = finding.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                        ? "red" : "orange1";

                    findingsTable.AddRow(
                        $"[bold {severityColor}]{Markup.Escape(finding.Severity)}[/]",
                        Markup.Escape(finding.FilePath),
                        finding.LineNumber.ToString(),
                        Markup.Escape(finding.Description.Length > 80
                            ? finding.Description[..77] + "..."
                            : finding.Description));
                }

                AnsiConsole.Write(findingsTable);
                AnsiConsole.WriteLine();
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[bold green]No issues found![/]");
            AnsiConsole.WriteLine();
        }
    }
}
