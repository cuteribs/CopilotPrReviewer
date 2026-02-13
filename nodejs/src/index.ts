#!/usr/bin/env node

import { program } from "commander";
import { defaultSettings, type AppSettings } from "./models.js";
import { runReview } from "./orchestrator.js";
import { printSummary } from "./reporter.js";
import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";

program
    .name("copilot-pr-reviewer")
    .description("AI-powered PR code reviewer for Azure DevOps using GitHub Copilot SDK")
    .argument("<pr-url>", "Azure DevOps pull request URL")
    .option("--pat <pat>", "Azure DevOps personal access token")
    .option("--auth-type <type>", "Authentication type: 'pat', 'oauth'")
    .option("--model <model>", "AI model to use for review")
    .option("--github-token <token>", "GitHub token for Copilot authentication")
    .option("--guidelines-path <path>", "Path to external guidelines folder")
    .option("--max-parallel <n>", "Maximum parallel batch reviews", parseInt)
    .option("--no-comments", "Skip posting comments to PR (dry run)")
    .option("--timeout <seconds>", "Timeout for the review process (in seconds)", parseInt)
    .option("--extend-review", "Review full code files in addition to diffs (default: diff-only mode)")
    .action(async (prUrl: string, opts: Record<string, unknown>) => {
        // Set env vars from CLI args
        if (opts.pat) process.env.AZURE_DEVOPS_PAT = opts.pat as string;
        if (opts.githubToken) process.env.GITHUB_TOKEN = opts.githubToken as string;

        // Load appsettings.json if present
        const settings = loadSettings(opts);

        try {
            const summary = await runReview(prUrl, settings);
            printSummary(summary);
        } catch (err) {
            console.error("Review failed:", (err as Error).message);
            process.exit(1);
        }
    });

program.parse();

function loadSettings(opts: Record<string, unknown>): AppSettings {
    const settings = structuredClone(defaultSettings);

    // Load from appsettings.json
    const settingsPath = join(process.cwd(), "appsettings.json");
    if (existsSync(settingsPath)) {
        try {
            const fileSettings = JSON.parse(readFileSync(settingsPath, "utf-8"));
            if (fileSettings.AzureDevOps) Object.assign(settings.azureDevOps, camelCaseKeys(fileSettings.AzureDevOps));
            if (fileSettings.Copilot) Object.assign(settings.copilot, camelCaseKeys(fileSettings.Copilot));
            if (fileSettings.Review) Object.assign(settings.review, camelCaseKeys(fileSettings.Review));
        } catch { /* ignore parse errors */ }
    }

    // Override from env vars (COPILOT_PR_REVIEWER_ prefix)
    const envModel = process.env.COPILOT_PR_REVIEWER_Copilot__Model;
    if (envModel) settings.copilot.model = envModel;
    const envMaxParallel = process.env.COPILOT_PR_REVIEWER_Copilot__MaxParallelBatches;
    if (envMaxParallel) settings.copilot.maxParallelBatches = parseInt(envMaxParallel);

    // Override from CLI args
    if (opts.authType) settings.azureDevOps.authType = opts.authType as string;
    if (opts.model) settings.copilot.model = opts.model as string;
    if (opts.timeout) settings.copilot.timeoutSeconds = opts.timeout as number;
    if (opts.guidelinesPath) settings.review.guidelinesPath = opts.guidelinesPath as string;
    if (opts.maxParallel) settings.copilot.maxParallelBatches = opts.maxParallel as number;
    if (opts.comments === false) settings.review.postComments = false;
    if (opts.extendReview) settings.review.extendReview = true;

    return settings;
}

function camelCaseKeys(obj: Record<string, unknown>): Record<string, unknown> {
    const result: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(obj)) {
        const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        result[camelKey] = value;
    }
    return result;
}
