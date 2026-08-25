using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// Writes the comments filed on a case. Only the author edits or deletes their own (SDD-013).
/// </summary>
public interface ICommentWriter
{
    /// <summary>
    /// False where the tenant has no such case.
    /// </summary>
    public Task<bool> AddCaseComment(Guid caseId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token);
}
