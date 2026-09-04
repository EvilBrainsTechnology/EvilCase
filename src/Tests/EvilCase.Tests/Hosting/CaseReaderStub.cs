using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Hosting;

internal static class CaseReaderStub
{
    public const string Title = "Přestupek — překročení rychlosti";

    public static ICaseReader WithOneCase()
    {
        var reader = Substitute.For<ICaseReader>();
        reader
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns([
                new CaseListItem
                {
                    CaseId = Guid.CreateVersion7(),
                    CaseNumber = "EC/20260821-001",
                    Title = Title,
                    Date = new DateOnly(2026, 1, 1),
                    Status = CaseStatus.Active,
                    Changed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ]);
        reader
            .CountCasesByStatus(Arg.Any<CancellationToken>())
            .Returns(new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 });

        return reader;
    }
}
