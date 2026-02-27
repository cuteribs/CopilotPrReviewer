import { readFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import type { TechStack } from "./models.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
const resourcesDir = join(__dirname, "..", "resources");

const resourceNames: Partial<Record<TechStack, string>> = {
    Dotnet: "dotnet-guidelines.md",
    Frontend: "frontend-guidelines.md",
    Python: "python-guidelines.md",
};

const cache = new Map<string, string>();

export function getGuidelines(techStack: TechStack, guidelinesPath?: string): string | undefined {
    const cacheKey = `${techStack}:${guidelinesPath ?? ""}`;
    if (cache.has(cacheKey)) return cache.get(cacheKey);

    const fileName = resourceNames[techStack];
    if (!fileName) return undefined;

    // Try external path first
    if (guidelinesPath) {
        const externalPath = join(guidelinesPath, fileName);
        if (existsSync(externalPath)) {
            const content = readFileSync(externalPath, "utf-8");
            cache.set(cacheKey, content);
            return content;
        }
    }

    // Fall back to embedded resources
    const embeddedPath = join(resourcesDir, fileName);
    if (existsSync(embeddedPath)) {
        const content = readFileSync(embeddedPath, "utf-8");
        cache.set(cacheKey, content);
        return content;
    }

    return undefined;
}

export function getStrategy(extendReview: boolean, guidelinesPath?: string): string | undefined {
    const fileName = extendReview ? "extended-pr-review-strategy.md" : "pr-review-strategy.md";

    // Try external path first
    if (guidelinesPath) {
        const externalPath = join(guidelinesPath, fileName);
        if (existsSync(externalPath)) {
            const content = readFileSync(externalPath, "utf-8");
            return content;
        }
    }

    // Fall back to embedded resources
    const embeddedPath = join(resourcesDir, fileName);
    if (existsSync(embeddedPath)) {
        const content = readFileSync(embeddedPath, "utf-8");
        return content;
    }

    return undefined;
}

export function getOutputFormat(): string {
    const formatPath = join(resourcesDir, "output-format.md");
    if (existsSync(formatPath)) {
        return readFileSync(formatPath, "utf-8");
    }
    return "!!MISSING OUTPUT FORMAT!!";
}
