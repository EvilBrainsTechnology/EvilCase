namespace EvilBrains.EvilCase.Api.Contract.Comments;

public sealed record CommentItem
{
    public required Guid Id { get; init; }

    public required string Body { get; init; }

    /// <summary>
    /// The author's e-mail. A comment is the only place a user is named (SDD-013).
    /// </summary>
    public required string AuthorEmail { get; init; }

    /// <summary>
    /// True where the signed-in user wrote it; only then may it be edited or deleted.
    /// </summary>
    public required bool IsAuthor { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }
}
