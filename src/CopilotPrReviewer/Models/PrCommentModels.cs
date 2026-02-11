namespace CopilotPrReviewer.Models;

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
