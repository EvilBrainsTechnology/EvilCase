using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseCommentWriter(ApplicationDbContext context, IOwnerContext owner, TimeProvider time) : ICaseCommentWriter
{
    public async Task<CaseComment?> Add(long caseId, AddCaseCommentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = time.GetUtcNow().UtcDateTime;

        // One statement scopes the write to the caller's own case, says whether there is one, and moves
        // the case up the list.
        var written = await context.Cases
            .WithId(caseId)
            .OwnedBy(owner)
            .ExecuteUpdateAsync(set => set.SetProperty(@case => @case.Updated, now), cancellationToken);

        if (written == 0)
            return null;

        var comment = new Comment
        {
            CaseId = caseId,
            Body = request.Body.Trim(),
            AuthorUserId = owner.OwnerId,
            Created = now,
        };

        _ = context.Comments.Add(comment);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Read back through the same projection the thread is read with, so both say the same thing.
        return await context.Comments
            .Where(saved => saved.Id == comment.Id)
            .AsCaseComments()
            .SingleAsync(cancellationToken);
    }
}
