using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DashboardViewTests
{
    [Test]
    public void ATenantWithNoCaseIsEmpty()
    {
        var view = new DashboardView
        {
            Counts = new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 0 },
            ChangedCases = [],
            RecentActs = [],
        };

        Assert.That(view.IsEmpty, Is.True, "a tenant with no case at all leads to creating the first one");
    }

    [Test]
    public void ATenantWithCasesButNoActKeepsItsTiles()
    {
        var view = new DashboardView
        {
            Counts = new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 },
            ChangedCases = [ChangedCase()],
            RecentActs = [],
        };

        Assert.That(view.IsEmpty, Is.False, "an empty act list is an empty tile, not the dashboard's empty state");
    }

    [Test]
    public void AClosedCaseAloneStillCounts()
    {
        var view = new DashboardView
        {
            Counts = new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 1 },
            ChangedCases = [ChangedCase(CaseStatus.Closed)],
            RecentActs = [],
        };

        Assert.That(view.IsEmpty, Is.False, "a tenant whose only case is closed still holds a case");
    }

    private static CaseListItem ChangedCase(CaseStatus status = CaseStatus.Active)
    {
        return new CaseListItem
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            Title = "Přestupek",
            Date = new DateOnly(2026, 8, 21),
            Status = status,
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
