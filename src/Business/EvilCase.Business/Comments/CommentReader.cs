using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Comments;

internal sealed class CommentReader(IDbSession dbSession, IUserContext userContext) : ICommentReader
{
    public async Task<IReadOnlyList<CommentItem>> ListCaseComments(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        return await context.Comments
            .OnCase(caseId)
            .AsCommentItems(context.Users, userContext.UserId)
            .InDiaryOrder()
            .ToListAsync(token);
    }
}
