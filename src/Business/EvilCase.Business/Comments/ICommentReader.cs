using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// Reads the comments filed on a case.
/// </summary>
public interface ICommentReader
{
    public Task<IReadOnlyList<CommentItem>> ListCaseComments(Guid caseId, CancellationToken token);
}
