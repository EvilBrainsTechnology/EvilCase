using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Lists;

public class ListQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task ThePageSkipsAndTakesExactlyWhatTheRequestAsksFor()
    {
        await this.AddCases(5);

        var page = await this.List(new CaseListRequest { Skip = 1, Take = 2, SortDirection = ListSortDirection.Ascending });

        string[] expected = ["Případ 2", "Případ 3"];

        Assert.That(page.Items.Select(static item => item.Title), Is.EqualTo(expected), "the page starts where Skip says and holds what Take says");
    }

    [Test]
    public async Task TheTotalCountCountsWhatTheFilterLeavesAndIgnoresThePage()
    {
        await this.AddCases(5);
        await this.Tenant.AddCase(Day, "Odvolání");

        var paged = await this.List(new CaseListRequest { Take = 2 });
        var filtered = await this.List(new CaseListRequest { Search = "odvolání", Take = 2 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(paged.Items, Has.Count.EqualTo(2));
            Assert.That(paged.TotalCount, Is.EqualTo(6), "the total counts every row the filter leaves, not the page");
            Assert.That(filtered.TotalCount, Is.EqualTo(1), "the filter narrows the total with the list");
        }
    }

    [Test]
    public async Task TheTotalCountNeverCountsAnotherTenantsRows()
    {
        await this.Tenant.AddCase(Day, "Moje věc");

        await using (var other = await TestTenant.Create())
        {
            await other.AddCase(Day, "Cizí věc");
            await other.AddCase(Day, "Další cizí věc");
        }

        var page = await this.List(new CaseListRequest { Take = 20 });

        Assert.That(page.TotalCount, Is.EqualTo(1), "the tenant query filter is what keeps another tenant's rows out of the total");
    }

    /// <summary>
    /// The database stamps <c>Created</c> off the clock, so two rows never share it and no result
    /// reaches the identifier behind it.
    /// </summary>
    [Test]
    public void TheWriteMomentAndTheIdentifierMakeEverySortOrderTotal()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var sort in Enum.GetValues<CaseSortKey>())
            {
                var sql = this.Tenant.Context.Cases.InSortOrder(sort, ListSortDirection.Descending).ToQueryString();
                var orderBy = sql[sql.LastIndexOf("ORDER BY", StringComparison.Ordinal)..];

                Assert.That(orderBy, Does.Contain("\"Created\" DESC"), $"{sort}: the write moment breaks a tie on the sort key");
                Assert.That(orderBy, Does.Contain("\"Id\" DESC"), $"{sort}: the identifier makes the order total");
            }
        }
    }

    [Test]
    public async Task ReversingTheDirectionReversesTheTieBreakToo()
    {
        var caseIds = TestTenant.SortedEntityIds(2);

        var written = await this.Tenant.AddCase(Day, "Zapsáno dřív", caseId: caseIds[1]);
        var writtenLater = await this.Tenant.AddCase(Day, "Zapsáno později", caseId: caseIds[0]);

        var descending = await this.List(new CaseListRequest { Take = 20 });
        var ascending = await this.List(new CaseListRequest { Take = 20, SortDirection = ListSortDirection.Ascending });

        Guid[] expectedDescending = [writtenLater.Id, written.Id];
        Guid[] expectedAscending = [written.Id, writtenLater.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(descending.Items.Select(static item => item.CaseId), Is.EqualTo(expectedDescending));
            Assert.That(ascending.Items.Select(static item => item.CaseId), Is.EqualTo(expectedAscending), "the tie-break turns with the direction, so the two orders are exact opposites");
        }
    }

    [Test]
    public async Task NoRowAppearsOnTwoPagesOfTheSameSortedList()
    {
        await this.AddCases(6);

        var first = await this.List(new CaseListRequest { Take = 2 });
        var second = await this.List(new CaseListRequest { Skip = 2, Take = 2 });
        var third = await this.List(new CaseListRequest { Skip = 4, Take = 2 });

        var walked = first.Items.Concat(second.Items).Concat(third.Items).Select(static item => item.CaseId).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(walked, Is.Unique, "a total order is what keeps a row off two pages");
            Assert.That(walked, Has.Count.EqualTo(first.TotalCount), "walking the pages reaches every row the total counts");
        }
    }

    private async Task AddCases(int count)
    {
        for (var index = 1; index <= count; index++)
            await this.Tenant.AddCase(Day, $"Případ {index.ToString(CultureInfo.InvariantCulture)}");
    }

    private async Task<CaseListResponse> List(CaseListRequest request)
    {
        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        return await reader.ListCases(request, CancellationToken.None);
    }
}
