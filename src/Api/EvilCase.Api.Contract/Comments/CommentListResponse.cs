namespace EvilBrains.EvilCase.Api.Contract.Comments;

public sealed record CommentListResponse
{
    public required IReadOnlyList<CommentItem> Items { get; init; }
}
