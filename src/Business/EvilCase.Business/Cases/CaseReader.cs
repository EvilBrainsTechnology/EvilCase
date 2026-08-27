using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession dbSession) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token)
    {
        return await dbSession.Current.Cases
            .WithStatus(request.Status)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        var @case = await context.Cases.DetailOf(caseId, token);
        if (@case is null)
            return null;

        var children = await context.Cases
            .WithParent(caseId)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);

        var numbers = await context.ExternalCaseNumbers
            .OfCase(caseId)
            .InAssignmentOrder()
            .AsItems()
            .ToListAsync(token);

        return @case with { ChildCases = children, ExternalNumbers = numbers };
    }
}
