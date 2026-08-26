using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;

namespace EvilBrains.EvilCase.Tests.Controllers;

internal sealed class RecordingCommentWriter : ICommentWriter
{
    public Guid? AddCaseId { get; private set; }

    public Guid? AddActId { get; private set; }

    public CommentEditRequest? AddRequest { get; private set; }

    public CommentWriteOutcome AddOutcome { get; init; }

    public Guid? UpdateCaseId { get; private set; }

    public Guid? UpdateActId { get; private set; }

    public Guid? UpdateCommentId { get; private set; }

    public CommentEditRequest? UpdateRequest { get; private set; }

    public CommentWriteOutcome UpdateOutcome { get; init; }

    public Guid? DeleteCaseId { get; private set; }

    public Guid? DeleteActId { get; private set; }

    public Guid? DeleteCommentId { get; private set; }

    public CommentWriteOutcome DeleteOutcome { get; init; }

    public Task<CommentWriteOutcome> AddCaseComment(Guid caseId, CommentEditRequest request, CancellationToken token)
    {
        this.AddCaseId = caseId;
        this.AddRequest = request;

        return Task.FromResult(this.AddOutcome);
    }

    public Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        this.UpdateCaseId = caseId;
        this.UpdateCommentId = commentId;
        this.UpdateRequest = request;

        return Task.FromResult(this.UpdateOutcome);
    }

    public Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token)
    {
        this.DeleteCaseId = caseId;
        this.DeleteCommentId = commentId;

        return Task.FromResult(this.DeleteOutcome);
    }

    public Task<CommentWriteOutcome> AddActComment(Guid caseId, Guid actId, CommentEditRequest request, CancellationToken token)
    {
        this.AddCaseId = caseId;
        this.AddActId = actId;
        this.AddRequest = request;

        return Task.FromResult(this.AddOutcome);
    }

    public Task<CommentWriteOutcome> UpdateActComment(Guid caseId, Guid actId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        this.UpdateCaseId = caseId;
        this.UpdateActId = actId;
        this.UpdateCommentId = commentId;
        this.UpdateRequest = request;

        return Task.FromResult(this.UpdateOutcome);
    }

    public Task<CommentWriteOutcome> DeleteActComment(Guid caseId, Guid actId, Guid commentId, CancellationToken token)
    {
        this.DeleteCaseId = caseId;
        this.DeleteActId = actId;
        this.DeleteCommentId = commentId;

        return Task.FromResult(this.DeleteOutcome);
    }
}
