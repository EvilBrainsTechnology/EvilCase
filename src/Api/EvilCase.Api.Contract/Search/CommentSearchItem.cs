namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record CommentSearchItem
{
    public required Guid CommentId { get; init; }

    public required string Body { get; init; }

    public required DateTime Created { get; init; }

    /// <summary>
    /// Exactly one of CaseId and ActId is set: the owner the comment hangs on.
    /// </summary>
    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    public required string OwnerTitle { get; init; }
}
