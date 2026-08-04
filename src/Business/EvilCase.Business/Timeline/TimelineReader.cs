using EvilBrains.EvilCase.Api.Contract.Timeline;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Timeline;

internal sealed class TimelineReader(ApplicationDbContext dbContext) : ITimelineReader
{
    /// <summary>
    /// One statement for a whole sub-tree, to any depth. EF has no recursive query of its own.
    /// </summary>
    private const string DescendantIds =
        """
        WITH RECURSIVE tree AS (
            SELECT "Id" FROM "Cases" WHERE "Id" = {0}
            UNION ALL
            SELECT c."Id" FROM "Cases" c JOIN tree ON c."ParentCaseId" = tree."Id"
        )
        SELECT "Id" FROM tree
        """;

    public async Task<IReadOnlyList<TimelineEntry>> Read(long caseId, bool includeDescendants, CancellationToken cancellationToken = default)
    {
        var caseIds = includeDescendants
            ? await dbContext.Database.SqlQueryRaw<long>(DescendantIds, caseId).ToListAsync(cancellationToken)
            : [caseId];

        var acts = await dbContext.Acts
            .Include(act => act.Case)
            .Where(act => caseIds.Contains(act.CaseId))
            .ToListAsync(cancellationToken);

        // Both directions at once: a note on an act is reached through the act.
        var comments = await dbContext.Comments
            .Include(comment => comment.Case)
            .Include(comment => comment.Act)
            .ThenInclude(act => act!.Case)
            .Where(comment => (comment.CaseId != null && caseIds.Contains(comment.CaseId.Value))
                || (comment.Act != null && caseIds.Contains(comment.Act.CaseId)))
            .ToListAsync(cancellationToken);

        return CaseTimeline.Merge(acts, comments);
    }
}
