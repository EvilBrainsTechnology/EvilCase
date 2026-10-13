using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Comments;

internal sealed class CommentReader(IDbSession dbSession, IUserContext userContext) : ICommentReader
{
    public async Task<IReadOnlyList<CommentItem>?> ListCaseComments(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        var caseExists = await context.Cases.Exists(caseId, token);
        if (!caseExists)
            return null;

        return await context.Comments
            .OnCase(caseId)
            .AsCommentItems(context.Users, userContext.UserId)
            .InDiaryOrder()
            .ToListAsync(token);
    }

    public async Task<IReadOnlyList<CommentItem>?> ListActComments(Guid actId, CancellationToken token)
    {
        var context = dbSession.Current;

        var actExists = await context.Acts.Exists(actId, token);
        if (!actExists)
            return null;

        return await context.Comments
            .OnAct(actId)
            .AsCommentItems(context.Users, userContext.UserId)
            .InDiaryOrder()
            .ToListAsync(token);
    }
}
