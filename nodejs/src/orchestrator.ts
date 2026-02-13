import type { PrInfo, ReviewFinding, ReviewSummary, PrCommentOptions, AppSettings } from "./models.js";
import { AzureDevOpsClient } from "./adoClient.js";
import { createBatches } from "./batchBuilder.js";
import { reviewBatch } from "./reviewService.js";

export async function runReview(prUrl: string, settings: AppSettings): Promise<ReviewSummary> {
    const startTime = Date.now();

    const adoClient = new AzureDevOpsClient(settings.azureDevOps.baseUrl, settings.azureDevOps.authType);

    // 1. Parse URL
    const prInfo = adoClient.parsePrUrl(prUrl);
    console.log(`Reviewing PR #${prInfo.pullRequestId} in ${prInfo.organization}/${prInfo.project}/${prInfo.repository}`);

    // 2. Fetch PR metadata
    const prDetails = await adoClient.getPrDetails(prInfo);
    console.log(`PR: ${prDetails.title} (${prDetails.sourceRefName.replace("refs/heads/", "")} → ${prDetails.targetRefName.replace("refs/heads/", "")})`);

    // 3. Fetch file changes
    const filePatches = await adoClient.fetchPrChanges(prInfo, prDetails);
    console.log(`Fetched ${filePatches.length} file changes`);

    // 4. Classify + filter + batch
    const batchResult = createBatches(filePatches, settings.copilot, settings.review.extendReview);
    console.log(`Created ${batchResult.batches.length} batches, excluded ${batchResult.excludedFiles.length} files (review mode: ${settings.review.extendReview ? "full code" : "diff-only"})`);

    // 5. Review batches in parallel (with concurrency limit)
    const allFindings: ReviewFinding[] = [];
    const { maxParallelBatches } = settings.copilot;

    // Simple semaphore for concurrency limiting
    const batches = batchResult.batches;
    for (let i = 0; i < batches.length; i += maxParallelBatches) {
        const chunk = batches.slice(i, i + maxParallelBatches);
        const results = await Promise.all(
            chunk.map(async (batch) => {
                console.log(`Reviewing batch ${batch.batchNumber} (${batch.techStack}, ${batch.files.size} files, ~${batch.totalTokens} tokens)`);
                const findings = await reviewBatch(batch, settings);
                console.log(`Batch ${batch.batchNumber} completed: ${findings.length} findings`);
                return findings;
            })
        );
        for (const findings of results) allFindings.push(...findings);
    }

    // 6. Post comments to ADO
    let commentsPosted = false;
    if (settings.review.postComments && allFindings.length > 0) {
        console.log(`Posting ${allFindings.length} comments to PR...`);
        await postFindings(adoClient, prInfo, allFindings);
        commentsPosted = true;
    }

    const duration = Date.now() - startTime;

    return {
        prUrl,
        prTitle: prDetails.title ?? "Untitled",
        totalFiles: filePatches.length,
        reviewedFiles: batchResult.batches.reduce((sum, b) => sum + b.files.size, 0),
        excludedFiles: batchResult.excludedFiles.length,
        totalBatches: batchResult.batches.length,
        findings: allFindings,
        commentsPosted,
        duration,
    };
}

async function postFindings(adoClient: AzureDevOpsClient, prInfo: PrInfo, findings: ReviewFinding[]): Promise<void> {
    for (const finding of findings) {
        try {
            const commentText = finding.suggestion
                ? `${finding.description}\n\n\`\`\`suggestion\n${finding.suggestion}\n\`\`\``
                : finding.description;

            const options: PrCommentOptions = {
                commentText,
                filePath: finding.filePath,
                lineNumber: finding.lineNumber > 0 ? finding.lineNumber : 1,
                severity: finding.severity,
            };

            await adoClient.postPrComment(prInfo, options);
        } catch (err) {
            console.warn(`Failed to post comment for ${finding.filePath}:${finding.lineNumber}:`, (err as Error).message);
        }
    }
}
