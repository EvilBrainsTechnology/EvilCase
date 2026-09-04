namespace EvilBrains.EvilCase.Api.Contract.Comments;

public sealed record CommentItem
{
    public required Guid CommentId { get; init; }

    public required string Body { get; init; }

    public required string AuthorEmail { get; init; }

    public required bool IsAuthor { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }
}
