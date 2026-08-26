using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// Reads the comments filed on a case or on an act.
/// </summary>
public interface ICommentReader
{
    public Task<IReadOnlyList<CommentItem>> ListCaseComments(Guid caseId, CancellationToken token);

    public Task<IReadOnlyList<CommentItem>> ListActComments(Guid caseId, Guid actId, CancellationToken token);
}
