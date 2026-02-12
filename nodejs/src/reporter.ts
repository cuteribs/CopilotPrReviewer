import type { ReviewSummary } from "./models.js";

export function printSummary(summary: ReviewSummary): void {
    console.log("");
    console.log("═".repeat(60));
    console.log("  Review Summary");
    console.log("═".repeat(60));
    console.log("");

    const rows = [
        ["PR URL", summary.prUrl],
        ["Title", summary.prTitle],
        ["Total Files", String(summary.totalFiles)],
        ["Reviewed Files", String(summary.reviewedFiles)],
        ["Excluded Files", String(summary.excludedFiles)],
        ["Batches", String(summary.totalBatches)],
        ["Total Findings", String(summary.findings.length)],
        ["Comments Posted", summary.commentsPosted ? "Yes" : "No"],
        ["Duration", `${(summary.duration / 1000).toFixed(1)}s`],
    ];

    const maxKey = Math.max(...rows.map(r => r[0].length));
    for (const [key, value] of rows) {
        console.log(`  ${key.padEnd(maxKey + 2)} ${value}`);
    }
    console.log("");

    if (summary.findings.length === 0) {
        console.log("  ✅ No issues found!");
        console.log("");
        return;
    }

    // Severity breakdown
    const critical = summary.findings.filter(f => f.severity.toLowerCase() === "critical").length;
    const major = summary.findings.filter(f => f.severity.toLowerCase() === "major").length;
    const minor = summary.findings.filter(f => f.severity.toLowerCase() === "minor").length;

    console.log("  Severity Breakdown:");
    if (critical > 0) console.log(`    🔴 Critical: ${critical}`);
    if (major > 0) console.log(`    🟠 Major:    ${major}`);
    if (minor > 0) console.log(`    🟡 Minor:    ${minor}`);
    console.log("");

    // Top findings
    const topFindings = summary.findings
        .filter(f => ["critical", "major"].includes(f.severity.toLowerCase()))
        .slice(0, 10);

    if (topFindings.length > 0) {
        console.log("  Top Critical/Major Findings:");
        for (const f of topFindings) {
            const emoji = f.severity.toLowerCase() === "critical" ? "🔴" : "🟠";
            const desc = f.description.length > 80 ? f.description.slice(0, 77) + "..." : f.description;
            console.log(`    ${emoji} [${f.severity}] ${f.filePath}:${f.lineNumber} — ${desc}`);
        }
        console.log("");
    }
}
