using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CasePathQuery
{
    /// <summary>
    /// The case and every case above it, the root first.
    /// </summary>
    public static async Task<IReadOnlyList<CaseCrumb>> PathTo(this IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        var path = new List<CaseCrumb>();

        // A case already on the path ends the walk: a loop already in the data would otherwise never end.
        for (Guid? next = caseId; next is { } id && path.TrueForAll(crumb => crumb.CaseId != id);)
        {
            var step = await cases
                .WithId(id)
                .Select(static @case => new { @case.Title, @case.ParentCaseId })
                .SingleAsync(token);

            path.Add(new CaseCrumb { CaseId = id, Title = step.Title });
            next = step.ParentCaseId;
        }

        path.Reverse();

        return path;
    }
}
