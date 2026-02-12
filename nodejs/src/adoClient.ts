import type {
    PrInfo, PullRequest, FilePatch, PrCommentOptions, PrThread,
    CommitDiffs, GitChange, AuthOptions,
} from "./models.js";
import { diffLines, type Change } from "diff";

const API_VERSION = "7.1";

export class AzureDevOpsClient {
    private baseUrl: string;
    private authOptions?: AuthOptions;
    private authType?: string;

    constructor(baseUrl: string, authType?: string) {
        this.baseUrl = baseUrl;
        this.authType = authType;
    }

    parsePrUrl(prUrl: string): PrInfo {
        const devAzurePattern = /https:\/\/dev\.azure\.com\/(.+?)\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;
        const vsPattern = /https:\/\/(.+?)\.visualstudio\.com\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;

        for (const pattern of [devAzurePattern, vsPattern]) {
            const match = prUrl.match(pattern);
            if (match) {
                return {
                    organization: match[1],
                    project: match[2],
                    repository: match[3],
                    pullRequestId: parseInt(match[4], 10),
                };
            }
        }
        throw new Error(`Invalid Azure DevOps PR URL format: ${prUrl}`);
    }

    async getPrDetails(prInfo: PrInfo): Promise<PullRequest> {
        const apiBase = this.getApiBase(prInfo);
        const url = `${apiBase}/pullRequests/${prInfo.pullRequestId}?api-version=${API_VERSION}`;
        const pr = await this.sendRequest<PullRequest>(url);

        if (pr.status.toLowerCase() !== "active")
            throw new Error("The PR is not active.");
        if (pr.mergeStatus.toLowerCase() !== "succeeded")
            throw new Error("The PR has merge conflict.");

        return pr;
    }

    async fetchPrChanges(prInfo: PrInfo, pr: PullRequest): Promise<FilePatch[]> {
        const apiBase = this.getApiBase(prInfo);
        const sourceBranch = encodeURIComponent(pr.sourceRefName.replace("refs/heads/", ""));
        const targetBranch = encodeURIComponent(pr.targetRefName.replace("refs/heads/", ""));

        const diffsUrl = `${apiBase}/diffs/commits?baseVersion=${targetBranch}&targetVersion=${sourceBranch}&$top=2000&api-version=${API_VERSION}`;
        const data = await this.sendRequest<CommitDiffs>(diffsUrl);
        const changes = data.changes ?? [];

        if (changes.length === 0) throw new Error("No changed files found in this PR.");

        const fileItems = changes.filter(c => this.isSupportedChange(c));
        if (fileItems.length === 0) throw new Error("No supported code file found in this PR.");

        return Promise.all(fileItems.map(c => this.getFilePatch(c, apiBase)));
    }

    async postPrComment(prInfo: PrInfo, options: PrCommentOptions): Promise<void> {
        const apiBase = this.getApiBase(prInfo);
        const threadUrl = `${apiBase}/pullRequests/${prInfo.pullRequestId}/threads?api-version=${API_VERSION}`;

        const thread: PrThread = {
            comments: [{ content: options.commentText }],
            threadContext: {
                filePath: options.filePath,
                rightFileStart: { line: options.lineNumber, offset: 1 },
                rightFileEnd: { line: options.lineNumber, offset: 999 },
            },
        };

        await this.sendRequest(threadUrl, "POST", thread);
    }

    // Private helpers
    private getApiBase(prInfo: PrInfo): string {
        return `${this.baseUrl}/${prInfo.organization}/${prInfo.project}/_apis/git/repositories/${prInfo.repository}`;
    }

    private isSupportedChange(change: GitChange): boolean {
        const supported = ["add", "edit", "delete", "rename"];
        return supported.includes(change.changeType.toLowerCase())
            && change.item.gitObjectType.toLowerCase() === "blob"
            && !!change.item.path
            && !!change.item.url;
    }

    private async getFilePatch(change: GitChange, apiBase: string): Promise<FilePatch> {
        const item = change.item;
        let sourceContent: string | undefined;
        let newContent: string | undefined;

        if (item.originalObjectId) {
            sourceContent = await this.getBlobContent(`${apiBase}/blobs/${item.originalObjectId}?api-version=${API_VERSION}`);
        }
        if (item.objectId) {
            newContent = await this.getBlobContent(`${apiBase}/blobs/${item.objectId}?api-version=${API_VERSION}`);
        }

        const { patch, linesAdded, linesDeleted } = generateUnifiedDiff(item.path, sourceContent ?? "", newContent ?? "");

        return {
            filePath: item.path,
            sourceContent,
            newContent,
            patch,
            changeType: change.changeType,
            linesAdded,
            linesDeleted,
        };
    }

    private async getAuthHeaders(): Promise<Record<string, string>> {
        if (!this.authOptions) {
            this.authOptions = await this.resolveAuth();
        }
        const auth = this.authOptions;
        if (auth.type === "pat") {
            const encoded = Buffer.from(`:${auth.token}`).toString("base64");
            return { Authorization: `Basic ${encoded}`, Accept: "application/json" };
        }
        return { Authorization: `Bearer ${auth.token}`, Accept: "application/json" };
    }

    private async resolveAuth(): Promise<AuthOptions> {
        const pat = process.env.AZURE_DEVOPS_PAT;
        const authType = this.authType?.toLowerCase() ?? "";

        if (authType === "pat" || (!authType && pat)) {
            if (!pat) throw new Error("PAT not found. Provide via --pat or AZURE_DEVOPS_PAT env var.");
            return { type: "pat", token: pat };
        }

        // OAuth not implemented in Node.js version — use PAT
        if (pat) return { type: "pat", token: pat };
        throw new Error("Azure DevOps authentication required. Set AZURE_DEVOPS_PAT environment variable or use --pat.");
    }

    private async sendRequest<T = void>(url: string, method = "GET", body?: unknown): Promise<T> {
        const headers = await this.getAuthHeaders();
        const init: RequestInit = { method, headers };

        if (body && ["POST", "PUT", "PATCH"].includes(method)) {
            init.body = JSON.stringify(body);
            (init.headers as Record<string, string>)["Content-Type"] = "application/json";
        }

        const response = await fetch(url, init);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText} - ${url}`);
        }

        if (method === "POST" && response.status === 200 || response.headers.get("content-length") === "0") {
            return undefined as T;
        }

        return (await response.json()) as T;
    }

    private async getBlobContent(url: string): Promise<string | undefined> {
        try {
            const headers = await this.getAuthHeaders();
            headers.Accept = "text/plain";
            const response = await fetch(url, { headers });
            if (!response.ok) return undefined;
            return await response.text();
        } catch {
            return undefined;
        }
    }
}

export function generateUnifiedDiff(fileName: string, oldText: string, newText: string): { patch: string; linesAdded: number; linesDeleted: number } {
    const changes: Change[] = diffLines(oldText, newText);
    const lines: string[] = [];
    lines.push(`--- a/${fileName}`);
    lines.push(`+++ b/${fileName}`);

    let linesAdded = 0;
    let linesDeleted = 0;
    let oldLine = 1;
    let newLine = 1;
    let hunkLines: string[] = [];
    let hunkStart = 0;

    function flushHunk() {
        if (hunkLines.length > 0) {
            lines.push(`@@ -${hunkStart},${oldLine - hunkStart} +${hunkStart},${newLine - hunkStart} @@`);
            lines.push(...hunkLines);
            hunkLines = [];
        }
    }

    for (const change of changes) {
        const changeLines = change.value.replace(/\n$/, "").split("\n");

        if (change.added) {
            if (hunkLines.length === 0) hunkStart = newLine;
            for (const l of changeLines) {
                hunkLines.push(`+${l}`);
                linesAdded++;
                newLine++;
            }
        } else if (change.removed) {
            if (hunkLines.length === 0) hunkStart = oldLine;
            for (const l of changeLines) {
                hunkLines.push(`-${l}`);
                linesDeleted++;
                oldLine++;
            }
        } else {
            flushHunk();
            hunkStart = oldLine;
            const count = changeLines.length;
            oldLine += count;
            newLine += count;
        }
    }

    flushHunk();
    return { patch: lines.join("\n") + "\n", linesAdded, linesDeleted };
}
