using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DashboardViewTests
{
    [Test]
    public void ATenantWithNoCaseIsEmpty()
    {
        var view = new DashboardView { Counts = new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 0 } };

        Assert.That(view.IsEmpty, Is.True, "a tenant with no case at all leads to creating the first one");
    }

    [Test]
    public void ATenantWithOneCaseKeepsItsTiles()
    {
        var view = new DashboardView { Counts = new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 } };

        Assert.That(view.IsEmpty, Is.False, "an empty list tile is an empty tile, not the dashboard's empty state");
    }

    [Test]
    public void AClosedCaseAloneStillCounts()
    {
        var view = new DashboardView { Counts = new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 1 } };

        Assert.That(view.IsEmpty, Is.False, "a tenant whose only case is closed still holds a case");
    }
}
