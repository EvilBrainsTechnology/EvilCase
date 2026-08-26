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

        var caseExists = await context.Cases.WithId(caseId).AnyAsync(token);
        if (!caseExists)
            return CommentWriteOutcome.NotFound;

        var comment = new Comment { CaseId = caseId, Body = request.Body.Trim() };

        context.Comments.Add(comment);

        await context.SaveChangesAsync(token);

        logger.LogInformation("Comment {CommentId} was written on case {CaseId}", comment.Id, caseId);

        return CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> UpdateCaseComment(Guid caseId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        var body = request.Body.Trim();
        var userId = userContext.UserId;

        var comments = dbSession.Current.Comments.OnCase(caseId).WithId(commentId);

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(comment => comment.Body, body), token);

        return rows == 0 ? CommentWriteOutcome.NotFound : CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> DeleteCaseComment(Guid caseId, Guid commentId, CancellationToken token)
    {
        var userId = userContext.UserId;

        var comments = dbSession.Current.Comments.OnCase(caseId).WithId(commentId);

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return CommentWriteOutcome.NotFound;

        logger.LogInformation("Comment {CommentId} was removed from case {CaseId}", commentId, caseId);

        return CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> AddActComment(Guid caseId, Guid actId, CommentEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        var actExists = await context.Acts.OfCase(caseId).WithId(actId).AnyAsync(token);
        if (!actExists)
            return CommentWriteOutcome.NotFound;

        var comment = new Comment { ActId = actId, Body = request.Body.Trim() };

        context.Comments.Add(comment);

        await context.SaveChangesAsync(token);

        logger.LogInformation("Comment {CommentId} was written on act {ActId}", comment.Id, actId);

        return CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> UpdateActComment(Guid caseId, Guid actId, Guid commentId, CommentEditRequest request, CancellationToken token)
    {
        var body = request.Body.Trim();
        var userId = userContext.UserId;

        var comments = dbSession.Current.Comments.OnAct(caseId, actId).WithId(commentId);

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(comment => comment.Body, body), token);

        return rows == 0 ? CommentWriteOutcome.NotFound : CommentWriteOutcome.Written;
    }

    public async Task<CommentWriteOutcome> DeleteActComment(Guid caseId, Guid actId, Guid commentId, CancellationToken token)
    {
        var userId = userContext.UserId;

        var comments = dbSession.Current.Comments.OnAct(caseId, actId).WithId(commentId);

        var outcome = await Authorize(comments, userId, token);
        if (outcome != CommentWriteOutcome.Written)
            return outcome;

        var rows = await comments
            .Where(comment => comment.UserId == userId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return CommentWriteOutcome.NotFound;

        logger.LogInformation("Comment {CommentId} was removed from act {ActId}", commentId, actId);

        return CommentWriteOutcome.Written;
    }

    /// <summary>
    /// Written where the note exists and the caller wrote it; the write itself repeats the author in its
    /// filter, so no decision rests on the read alone.
    /// </summary>
    private static async Task<CommentWriteOutcome> Authorize(IQueryable<Comment> comments, Guid userId, CancellationToken token)
    {
        var authorId = await comments
            .Select(comment => (Guid?)comment.UserId)
            .SingleOrDefaultAsync(token);

        if (authorId is null)
            return CommentWriteOutcome.NotFound;

        return authorId == userId ? CommentWriteOutcome.Written : CommentWriteOutcome.NotAuthor;
    }
}
