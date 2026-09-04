using EvilBrains.EvilCase.Api.Contract.Comments;

namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// Only the author edits or deletes a comment (SDD-013).
/// </summary>
public interface ICommentWriter
{
    public Task<CommentWriteOutcome> AddCaseComment(Guid caseId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token);

    public Task<CommentWriteOutcome> AddActComment(Guid caseId, Guid actId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> UpdateActComment(Guid caseId, Guid actId, Guid commentId, CommentEditRequest request, CancellationToken token);

    public Task<CommentWriteOutcome> DeleteActComment(Guid caseId, Guid actId, Guid commentId, CancellationToken token);
}
