using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes what one case detail reads, one composable step per rule.
/// </summary>
public static class CaseDetailQuery
{
    public static IQueryable<Case> WithId(this IQueryable<Case> cases, long id)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Where(@case => @case.Id == id);
    }

    /// <summary>
    /// The case, every ancestor up to its root and its whole sub-tree. Recursion is what LINQ cannot
    /// express, so the walk is a recursive CTE and the rest composes over it. Each branch carries the
    /// identifiers it came through and stops on one it has already been to, so a chain that closes a cycle
    /// ends without the walk bounding how deep a case may nest.
    /// </summary>
    public static IQueryable<Case> AroundCase(this DbSet<Case> cases, long id)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.FromSql(
            $"""
             WITH RECURSIVE "Ancestry" AS (
                 SELECT *, 0 AS "Depth", ARRAY["Id"] AS "Walked" FROM "Cases" WHERE "Id" = {id}
                 UNION ALL
                 SELECT parent.*, child."Depth" + 1, child."Walked" || parent."Id"
                 FROM "Cases" AS parent
                 INNER JOIN "Ancestry" AS child ON child."ParentCaseId" = parent."Id"
                 WHERE NOT parent."Id" = ANY (child."Walked")
             ),
             "SubTree" AS (
                 SELECT *, 0 AS "Depth", ARRAY["Id"] AS "Walked" FROM "Cases" WHERE "Id" = {id}
                 UNION ALL
                 SELECT child.*, parent."Depth" + 1, parent."Walked" || child."Id"
                 FROM "Cases" AS child
                 INNER JOIN "SubTree" AS parent ON child."ParentCaseId" = parent."Id"
                 WHERE NOT child."Id" = ANY (parent."Walked")
             )
             SELECT * FROM "Ancestry"
             UNION ALL
             SELECT * FROM "SubTree" WHERE "Depth" > 0
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
