using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes what one case detail reads, one composable step per rule.
/// </summary>
public static class CaseDetailQuery
{
    /// <summary>
    /// Where a parent chain closes a cycle the recursion would never end; nesting never comes near this.
    /// </summary>
    private const int MaxDistance = 64;

    public static IQueryable<Case> WithId(this IQueryable<Case> cases, long id)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Where(@case => @case.Id == id);
    }

    /// <summary>
    /// The case, every ancestor up to its root and its whole sub-tree. Recursion is what LINQ cannot
    /// express, so the walk is a recursive CTE and the rest composes over it.
    /// </summary>
    public static IQueryable<Case> AroundCase(this DbSet<Case> cases, long id)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.FromSql(
            $"""
             WITH RECURSIVE "Ancestry" AS (
                 SELECT *, 0 AS "Distance" FROM "Cases" WHERE "Id" = {id}
                 UNION ALL
                 SELECT parent.*, child."Distance" + 1
                 FROM "Cases" AS parent
                 INNER JOIN "Ancestry" AS child ON child."ParentCaseId" = parent."Id"
                 WHERE child."Distance" < {MaxDistance}
             ),
             "SubTree" AS (
                 SELECT *, 0 AS "Distance" FROM "Cases" WHERE "Id" = {id}
                 UNION ALL
                 SELECT child.*, parent."Distance" + 1
                 FROM "Cases" AS child
                 INNER JOIN "SubTree" AS parent ON child."ParentCaseId" = parent."Id"
                 WHERE parent."Distance" < {MaxDistance}
             )
             SELECT * FROM "Ancestry"
             UNION ALL
             SELECT * FROM "SubTree" WHERE "Distance" > 0
             """);
    }

    /// <summary>
    /// Reads only what a tree row shows, in one query.
    /// </summary>
    public static IQueryable<CaseGraphNode> AsGraphNodes(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Select(@case => new CaseGraphNode
        {
            Id = @case.Id,
            ParentCaseId = @case.ParentCaseId,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Status = @case.Status,
        });
    }

    /// <summary>
    /// Reads the case's own data, the tags among them, in one query. The tree and the thread hang off
    /// their own queries and the reader fills them in.
    /// </summary>
    public static IQueryable<CaseDetailResponse> AsDetails(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Select(@case => new CaseDetailResponse
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Subject = @case.Subject,
            Status = @case.Status,
            Tags = @case.Tags.OrderBy(tag => tag.Value).Select(tag => tag.Value).ToList(),
            Created = @case.Created,
            Updated = @case.Updated,
        });
    }
}
