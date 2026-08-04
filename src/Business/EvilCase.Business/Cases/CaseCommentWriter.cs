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

        if (!await context.Cases.WithId(caseId).AnyAsync(cancellationToken))
            return null;

        var comment = new Comment
        {
            CaseId = caseId,
            Body = request.Body.Trim(),
            AuthorUserId = owner.OwnerId,
            Created = time.GetUtcNow().UtcDateTime,
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
