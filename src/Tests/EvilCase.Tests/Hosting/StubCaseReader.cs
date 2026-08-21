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

    public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CaseListItem>>(
        [
            new CaseListItem
            {
                Id = Guid.CreateVersion7(),
                CaseNumber = "EC/20260821-001",
                Title = Title,
                Date = new DateOnly(2026, 1, 1),
                Status = CaseStatus.Active,
            },
        ]);
}
