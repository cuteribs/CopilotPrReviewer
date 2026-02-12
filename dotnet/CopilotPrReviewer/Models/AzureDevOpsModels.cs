namespace Cuteribs.CopilotPrReviewer.Models;

// PR URL parsed info
public record PrInfo
{
    public required string Organization { get; init; }
    public required string Project { get; init; }
    public required string Repository { get; init; }
    public required int PullRequestId { get; init; }
}

// PR metadata from ADO API
public record PullRequest
{
    public required int PullRequestId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string MergeStatus { get; init; }
    public required string SourceRefName { get; init; }
    public required string TargetRefName { get; init; }
    public string? CreationDate { get; init; }
    public PullRequestCreator? CreatedBy { get; init; }
}

public record PullRequestCreator
{
    public string? DisplayName { get; init; }
    public string? UniqueName { get; init; }
}

// Git diff API models
public record CommitDiffs
{
    public required GitChange[] Changes { get; init; }
}

public record GitChange
{
    public required GitItem Item { get; init; }
    public required string ChangeType { get; init; }
}

public record GitItem
{
    public string? ObjectId { get; init; }
    public string? OriginalObjectId { get; init; }
    public required string GitObjectType { get; init; }
    public required string CommitId { get; init; }
    public required string Path { get; init; }
    public bool? IsFolder { get; init; }
    public required string Url { get; init; }
}

public record FilePatch
{
    public required string FilePath { get; init; }
    public string? SourceContent { get; init; }
    public string? NewContent { get; init; }
    public required string Patch { get; init; }
    public required string ChangeType { get; init; }
    public int LinesAdded { get; init; }
    public int LinesDeleted { get; init; }
}

// PR comment threading
public record PrThread
{
    public required PrComment[] Comments { get; init; }
    public required ThreadContext ThreadContext { get; init; }
}

public record ThreadContext
{
    public required string FilePath { get; init; }
    public FilePosition? RightFileStart { get; init; }
    public FilePosition? RightFileEnd { get; init; }
}

public record FilePosition
{
    public required int Line { get; init; }
    public required int Offset { get; init; }
}

public record PrComment
{
    public required string Content { get; init; }
}

public record PrCommentOptions
{
    public required string CommentText { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public string? Severity { get; init; }
    public string? ThreadStatus { get; init; }
}

// Auth
public class AuthOptions
{
    public required string Type { get; init; }
    public required string Token { get; init; }
}
