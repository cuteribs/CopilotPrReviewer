namespace CopilotPrReviewer.Models;

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
