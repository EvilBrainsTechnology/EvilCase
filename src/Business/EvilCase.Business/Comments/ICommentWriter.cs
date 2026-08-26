using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// Writes the comments filed on a case or on an act. Only the author edits or deletes their own (SDD-013).
/// </summary>
public interface ICommentWriter
{
    /// <summary>
    /// False where the tenant has no such case.
    /// </summary>
    public Task<bool> AddCaseComment(Guid caseId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token);

    /// <summary>
    /// False where the tenant has no such act under that case.
    /// </summary>
    public Task<bool> AddActComment(Guid caseId, Guid actId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> UpdateActComment(Guid caseId, Guid actId, Guid commentId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> DeleteActComment(Guid caseId, Guid actId, Guid commentId, CancellationToken token);
}
