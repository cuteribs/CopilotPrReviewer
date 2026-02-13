// Enums
export enum TechStack {
    Dotnet = "Dotnet",
    Frontend = "Frontend",
    Python = "Python",
    Config = "Config",
}

export enum Severity {
    Critical = "Critical",
    Major = "Major",
    Minor = "Minor",
}

// Azure DevOps models
export interface PrInfo {
    organization: string;
    project: string;
    repository: string;
    pullRequestId: number;
}

export interface PullRequest {
    pullRequestId: number;
    title?: string;
    description?: string;
    status: string;
    mergeStatus: string;
    sourceRefName: string;
    targetRefName: string;
    creationDate?: string;
    createdBy?: { displayName?: string; uniqueName?: string };
}

export interface CommitDiffs {
    changes: GitChange[];
}

export interface GitChange {
    item: GitItem;
    changeType: string;
}

export interface GitItem {
    objectId?: string;
    originalObjectId?: string;
    gitObjectType: string;
    commitId: string;
    path: string;
    isFolder?: boolean;
    url: string;
}

export interface FilePatch {
    filePath: string;
    sourceContent?: string;
    newContent?: string;
    patch: string;
    changeType: string;
    linesAdded: number;
    linesDeleted: number;
}

export interface PrThread {
    comments: PrComment[];
    threadContext: ThreadContext;
}

export interface ThreadContext {
    filePath: string;
    rightFileStart?: FilePosition;
    rightFileEnd?: FilePosition;
}

export interface FilePosition {
    line: number;
    offset: number;
}

export interface PrComment {
    content: string;
}

export interface PrCommentOptions {
    commentText: string;
    filePath: string;
    lineNumber: number;
    severity?: string;
}

export interface AuthOptions {
    type: string;
    token: string;
}

// Review models
export interface ReviewBatch {
    batchNumber: number;
    techStack: TechStack;
    totalTokens: number;
    files: Map<string, BatchFile>;
}

export interface BatchFile {
    content: string;
    diff: string;
}

export interface BatchResult {
    batches: ReviewBatch[];
    excludedFiles: string[];
}

export interface ReviewFinding {
    filePath: string;
    lineNumber: number;
    severity: string;
    description: string;
    suggestion?: string;
}

export interface ReviewSummary {
    prUrl: string;
    prTitle: string;
    totalFiles: number;
    reviewedFiles: number;
    excludedFiles: number;
    totalBatches: number;
    findings: ReviewFinding[];
    commentsPosted: boolean;
    duration: number; // milliseconds
}

// Configuration
export interface AppSettings {
    azureDevOps: {
        authType?: string;
        baseUrl: string;
    };
    copilot: {
        model: string;
        maxTokensPerBatch: number;
        overheadTokens: number;
        maxParallelBatches: number;
        timeoutSeconds: number;
    };
    review: {
        guidelinesPath?: string;
        postComments: boolean;
        extendReview: boolean;
    };
}

export const defaultSettings: AppSettings = {
    azureDevOps: {
        baseUrl: "https://dev.azure.com",
    },
    copilot: {
        model: "gpt-5-mini",
        maxTokensPerBatch: 90_000,
        overheadTokens: 20_000,
        maxParallelBatches: 4,
        timeoutSeconds: 300,
    },
    review: {
        postComments: true,
        extendReview: false,
    },
};
