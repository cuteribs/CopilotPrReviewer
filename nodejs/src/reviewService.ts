import { CopilotClient } from "@github/copilot-sdk";
import type { ReviewBatch, ReviewFinding, AppSettings } from "./models.js";
import { getGuidelines, getReviewOutputFormat } from "./guidelines.js";

export async function reviewBatch(
    batch: ReviewBatch,
    settings: AppSettings,
): Promise<ReviewFinding[]> {
    const prompt = buildReviewPrompt(batch, settings.review.guidelinesPath);
    const githubToken = process.env.GITHUB_TOKEN ?? undefined;

    let client: any;
    try {
        client = new (CopilotClient as any)({ githubToken });
    } catch {
        // Some tests mock CopilotClient as a factory function (not constructible).
        // Fall back to calling it as a function when `new` is not supported.
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        client = (CopilotClient as any)({ githubToken });
    }

    try {
        const session = await client.createSession({ model: settings.copilot.model });

        let responseText = "";

        session.on((event: any) => {
            const ev = event as { type: string; data?: Record<string, unknown> };
            if (ev.type === "assistant.message" && ev.data?.content) {
                responseText += ev.data.content as string;
            } else if (ev.type === "session.error") {
                throw new Error((ev.data?.message as string) ?? "Copilot session error");
            }
        });

        await session.sendAndWait(
            { prompt },
            settings.copilot.timeoutSeconds * 1000,
        );

        await session.destroy();

            console.warn(responseText);
        return parseFindings(responseText);
    } finally {
        await client.stop();
    }
}

export function parseFindings(response: string): ReviewFinding[] {
    if (!response?.trim()) return [];

    const json = extractJson(response);
    if (!json) return [];

    try {
        const findings = JSON.parse(json) as ReviewFinding[];
        return Array.isArray(findings) ? findings : [];
    } catch {
        console.error("Failed to parse findings JSON");
        return [];
    }
}

export function extractJson(text: string): string | null {
    // Try ```json block
    let blockStart = text.indexOf("```json");
    if (blockStart >= 0) {
        const jsonStart = text.indexOf("[", blockStart);
        if (jsonStart >= 0) {
            const jsonEnd = text.lastIndexOf("]");
            if (jsonEnd > jsonStart) return text.slice(jsonStart, jsonEnd + 1);
        }
    }

    // Try bare ``` block
    blockStart = text.indexOf("```");
    if (blockStart >= 0) {
        const jsonStart = text.indexOf("[", blockStart);
        if (jsonStart >= 0) {
            const jsonEnd = text.lastIndexOf("]");
            if (jsonEnd > jsonStart) return text.slice(jsonStart, jsonEnd + 1);
        }
    }

    // Try raw JSON array
    const rawStart = text.indexOf("[");
    if (rawStart >= 0) {
        const rawEnd = text.lastIndexOf("]");
        if (rawEnd > rawStart) return text.slice(rawStart, rawEnd + 1);
    }

    return null;
}

function buildReviewPrompt(batch: ReviewBatch, guidelinesPath?: string): string {
    const parts: string[] = [];

    parts.push("You are an expert code reviewer. Review the following code changes and report issues.");
    parts.push("");

    const guidelines = getGuidelines(batch.techStack, guidelinesPath);
    if (guidelines) {
        parts.push("## Review Guidelines");
        parts.push(guidelines);
        parts.push("");
    }

    parts.push("## Files to Review");
    parts.push("");

    for (const [filePath, file] of batch.files) {
        parts.push(`### File: ${filePath}`);
        parts.push("");

        if (file.content) {
            parts.push("#### Full Content:");
            parts.push("```");
            parts.push(file.content);
            parts.push("```");
            parts.push("");
        }

        if (file.diff) {
            parts.push("#### Diff:");
            parts.push("```diff");
            parts.push(file.diff);
            parts.push("```");
            parts.push("");
        }
    }

    parts.push(getReviewOutputFormat());

    return parts.join("\n");
}
