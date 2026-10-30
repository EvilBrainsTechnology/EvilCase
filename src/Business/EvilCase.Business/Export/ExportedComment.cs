namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedComment
{
    public required Guid CommentId { get; init; }

    public required string Body { get; init; }

    public required string AuthorEmail { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime? Updated { get; init; }
}
