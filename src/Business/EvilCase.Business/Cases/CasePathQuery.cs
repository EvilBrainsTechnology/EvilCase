using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CasePathQuery
{
    /// <summary>
    /// The cases above the case, the root first.
    /// </summary>
    public static async Task<IReadOnlyList<CaseCrumb>> AncestorsOf(this IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        var ancestors = new List<CaseCrumb>();
        var next = await cases.WithId(caseId).Select(static @case => @case.ParentCaseId).SingleOrDefaultAsync(token);

        // A case already seen ends the walk: a loop already in the data would otherwise never end.
        while (next is { } id && id != caseId && ancestors.TrueForAll(crumb => crumb.CaseId != id))
        {
            var step = await cases
                .WithId(id)
                .Select(static @case => new { @case.Title, @case.ParentCaseId })
                .SingleOrDefaultAsync(token);

            if (step is null)
                break;

            ancestors.Add(new CaseCrumb { CaseId = id, Title = step.Title });
            next = step.ParentCaseId;
        }

        ancestors.Reverse();

        return ancestors;
    }
}
