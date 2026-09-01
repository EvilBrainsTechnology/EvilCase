using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Comments;

internal sealed class CommentWriter(IDbSession dbSession, IUserContext userContext, ILogger<CommentWriter> logger) : ICommentWriter
{
    public async Task<CommentWriteOutcome> AddCaseComment(Guid caseId, CommentEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var comment = new Comment { CaseId = caseId, Body = request.Body.Trim() };

        var caseExists = await context.Cases.Exists(caseId, token);
        if (!caseExists)
            return CommentWriteOutcome.NotFound;

        context.Comments.Add(comment);
        await context.SaveChangesAsync(token);
        logger.LogInformation("Comment {CommentId} was written on case {CaseId}", comment.Id, caseId);
        return CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        var comments = dbSession.Current.Comments.OnCase(caseId).WithId(commentId);

        var outcome = await this.UpdateComment(comments, request.Body.Trim(), token);
        if (outcome == CommentWriteOutcome.Written)
            logger.LogInformation("Comment {CommentId} was edited on case {CaseId}", commentId, caseId);

        return outcome;
    }

    public async Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token)
    {
        var comments = dbSession.Current.Comments.OnCase(caseId).WithId(commentId);

        var outcome = await this.DeleteComment(comments, token);
        if (outcome == CommentWriteOutcome.Written)
            logger.LogInformation("Comment {CommentId} was removed from case {CaseId}", commentId, caseId);

        return outcome;
    }

    public async Task<CommentWriteOutcome> AddActComment(Guid caseId, Guid actId, CommentEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var comment = new Comment { ActId = actId, Body = request.Body.Trim() };

        var actExists = await context.Acts.OfCase(caseId).Exists(actId, token);
        if (!actExists)
            return CommentWriteOutcome.NotFound;

        context.Comments.Add(comment);
        await context.SaveChangesAsync(token);
        logger.LogInformation("Comment {CommentId} was written on act {ActId}", comment.Id, actId);
        return CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> UpdateActComment(Guid caseId, Guid actId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        var comments = dbSession.Current.Comments.OnAct(caseId, actId).WithId(commentId);

        var outcome = await this.UpdateComment(comments, request.Body.Trim(), token);
        if (outcome == CommentWriteOutcome.Written)
            logger.LogInformation("Comment {CommentId} was edited on act {ActId}", commentId, actId);

        return outcome;
    }

    public async Task<CommentWriteOutcome> DeleteActComment(Guid caseId, Guid actId, Guid commentId, CancellationToken token)
    {
        var comments = dbSession.Current.Comments.OnAct(caseId, actId).WithId(commentId);

        var outcome = await this.DeleteComment(comments, token);
        if (outcome == CommentWriteOutcome.Written)
            logger.LogInformation("Comment {CommentId} was removed from act {ActId}", commentId, actId);

        return outcome;
    }

    private async Task<CommentWriteOutcome> UpdateComment(IQueryable<Comment> comments, string body, CancellationToken token)
    {
        var userId = userContext.UserId;

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(static comment => comment.Body, body), token);

        return rows == 0 ? CommentWriteOutcome.NotFound : CommentWriteOutcome.Written;
    }

    private async Task<CommentWriteOutcome> DeleteComment(IQueryable<Comment> comments, CancellationToken token)
    {
        var userId = userContext.UserId;

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteDeleteAsync(token);

        return rows == 0 ? CommentWriteOutcome.NotFound : CommentWriteOutcome.Written;
    }

    /// <summary>
    /// Written where the note exists and the caller wrote it; the write itself repeats the author in its
    /// filter, so no decision rests on the read alone.
    /// </summary>
    private static async Task<CommentWriteOutcome> Authorize(IQueryable<Comment> comments, Guid userId, CancellationToken token)
    {
        var authorId = await comments
            .Select(static comment => (Guid?)comment.UserId)
            .SingleOrDefaultAsync(token);

        if (authorId is null)
            return CommentWriteOutcome.NotFound;

        return authorId == userId ? CommentWriteOutcome.Written : CommentWriteOutcome.NotAuthor;
    }
}
