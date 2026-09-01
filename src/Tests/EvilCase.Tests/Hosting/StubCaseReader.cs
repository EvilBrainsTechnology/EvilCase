using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Stands in for the one type behind the case list that would open a database connection, so a host
/// test can reach an ordinary authenticated endpoint and see its body.
/// </summary>
internal sealed class StubCaseReader : ICaseReader
{
    public const string Title = "Přestupek — překročení rychlosti";

    public async Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token)
    {
        return
        [
            new CaseListItem
            {
                CaseId = Guid.CreateVersion7(),
                CaseNumber = "EC/20260821-001",
                Title = Title,
                Date = new DateOnly(2026, 1, 1),
                Status = CaseStatus.Active,
                Changed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        ];
    }

    public async Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token)
    {
        return null;
    }

    public async Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token)
    {
        return new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 };
    }
}
