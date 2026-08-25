using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The one case's header, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class CaseDetailQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task TheDetailCarriesTheNumberTheDateTheTitleTheDescriptionAndTheStatus()
    {
        var @case = await this.tenant.AddCase(
            Day,
            "Přestupek",
            description: "Popis přestupku",
            status: CaseStatus.WaitingOnAuthority);

        var detail = await this.tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        var expected = new CaseDetail
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Date = Day,
            Title = "Přestupek",
            Description = "Popis přestupku",
            Status = CaseStatus.WaitingOnAuthority,
        };

        Assert.That(detail, Is.EqualTo(expected), "the detail shows the case's number, date, title, description and status");
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var detail = await this.tenant.Context.Cases.DetailOf(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);

        var detail = await this.tenant.Context.Cases.DetailOf(otherCase.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }
}
