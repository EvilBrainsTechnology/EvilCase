using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

public interface ICommentReader
{
    /// <summary>
    /// Null where the case is unknown; empty where it has no comments.
    /// </summary>
    public Task<IReadOnlyList<CommentItem>?> ListCaseComments(Guid caseId, CancellationToken token);

    /// <summary>
    /// Null where the act is unknown; empty where it has no comments.
    /// </summary>
    public Task<IReadOnlyList<CommentItem>?> ListActComments(Guid actId, CancellationToken token);
}
