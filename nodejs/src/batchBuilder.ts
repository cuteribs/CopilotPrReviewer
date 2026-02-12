import { encode } from "gpt-tokenizer";
import { TechStack, type AppSettings, type BatchFile, type BatchResult, type FilePatch, type ReviewBatch } from "./models.js";
import { extname, basename } from "node:path";

const extensionMap: Record<string, TechStack> = {
    ".cs": TechStack.Dotnet, ".csproj": TechStack.Dotnet, ".sln": TechStack.Dotnet,
    ".slnx": TechStack.Dotnet, ".props": TechStack.Dotnet, ".razor": TechStack.Dotnet,
    ".cshtml": TechStack.Dotnet,
    ".js": TechStack.Frontend, ".jsx": TechStack.Frontend, ".ts": TechStack.Frontend,
    ".tsx": TechStack.Frontend, ".html": TechStack.Frontend, ".htm": TechStack.Frontend,
    ".css": TechStack.Frontend, ".scss": TechStack.Frontend, ".sass": TechStack.Frontend,
    ".less": TechStack.Frontend, ".vue": TechStack.Frontend, ".svelte": TechStack.Frontend,
    ".py": TechStack.Python, ".pyi": TechStack.Python, ".pyx": TechStack.Python,
    ".pxd": TechStack.Python,
    ".json": TechStack.Config, ".yaml": TechStack.Config, ".yml": TechStack.Config,
    ".http": TechStack.Config, ".rest": TechStack.Config,
};

const exactNameExclusions = new Set([
    "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
    "pipfile.lock", "poetry.lock", "packages.lock.json",
].map(n => n.toLowerCase()));

const suffixExclusions = [".min.js", ".min.css", ".d.ts", ".g.cs", ".designer.cs", ".generated.cs"];

export function classifyFile(filePath: string): TechStack | undefined {
    const ext = extname(filePath).toLowerCase();
    return extensionMap[ext];
}

export function shouldExclude(filePath: string): boolean {
    const fileName = basename(filePath).toLowerCase();
    if (exactNameExclusions.has(fileName)) return true;

    const lowerPath = filePath.toLowerCase();
    return suffixExclusions.some(suffix => lowerPath.endsWith(suffix));
}

export function countTokens(text: string): number {
    if (!text) return 0;
    return encode(text).length;
}

export function createBatches(filePatches: FilePatch[], settings: AppSettings["copilot"]): BatchResult {
    const availableTokens = settings.maxTokensPerBatch - settings.overheadTokens;
    const excluded: string[] = [];
    const stacks = new Map<TechStack, FilePatch[]>();

    for (const file of filePatches) {
        const stack = classifyFile(file.filePath);
        if (stack === undefined) {
            excluded.push(file.filePath);
            continue;
        }
        if (shouldExclude(file.filePath)) {
            excluded.push(file.filePath);
            continue;
        }
        if (!stacks.has(stack)) stacks.set(stack, []);
        stacks.get(stack)!.push(file);
    }

    // Sort files within each stack by size ascending
    for (const files of stacks.values()) {
        files.sort((a, b) =>
            ((a.newContent?.length ?? 0) + a.patch.length) -
            ((b.newContent?.length ?? 0) + b.patch.length)
        );
    }

    const batches: ReviewBatch[] = [];
    let batchNumber = 1;

    for (const [stack, files] of stacks) {
        let currentFiles = new Map<string, BatchFile>();
        let currentTokens = 0;

        for (const file of files) {
            const content = file.newContent ?? "";
            const diff = file.patch;
            const fileTokens = countTokens(content) + countTokens(diff);

            if (currentTokens + fileTokens > availableTokens && currentFiles.size > 0) {
                batches.push({ batchNumber: batchNumber++, techStack: stack, totalTokens: currentTokens, files: currentFiles });
                currentFiles = new Map();
                currentTokens = 0;
            }

            currentFiles.set(file.filePath, { content, diff });
            currentTokens += fileTokens;
        }

        if (currentFiles.size > 0) {
            batches.push({ batchNumber: batchNumber++, techStack: stack, totalTokens: currentTokens, files: currentFiles });
        }
    }

    return { batches, excludedFiles: excluded };
}
