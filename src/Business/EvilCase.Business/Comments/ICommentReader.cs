using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

public interface ICommentReader
{
    public Task<IReadOnlyList<CommentItem>> ListCaseComments(Guid caseId, CancellationToken token);

    public Task<IReadOnlyList<CommentItem>> ListActComments(Guid caseId, Guid actId, CancellationToken token);
}
