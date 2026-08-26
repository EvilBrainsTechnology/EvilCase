using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Filing a case against a real PostgreSQL. Each test seeds a tenant of its own, so none cleans up
/// after itself.
/// </summary>
public class CaseCreateTests
{
    private static readonly DateOnly ParentDay = new(2026, 8, 21);

    private static readonly DateOnly ChildDay = new(2026, 8, 22);

    private TestTenant tenant = null!;

    private CaseWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create(asHost: true);
        this.writer = new CaseWriter(new FixedDbSession(this.tenant.Context), new FakeCaseNumberIssuer(), new FakeFileBlobStore(), NullLogger<CaseWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task ASubordinateCaseIsFiledUnderItsParent()
    {
        var parent = await this.tenant.AddCase(ParentDay, "Rodič");

        var result = await this.writer.CreateCase(
            new CreateCaseRequest { Date = ChildDay, Title = "Podřízený", ParentCaseId = parent.Id },
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.Created));

        var reloaded = await this.tenant.Context.Cases.SingleAsync(@case => @case.Id == result.Case!.CaseId);

        Assert.That(reloaded.ParentCaseId, Is.EqualTo(parent.Id));
    }

    [Test]
    public async Task AParentThatIsNoCaseOfTheTenantFilesNothing()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(ParentDay, "Cizí spis");

        var result = await this.writer.CreateCase(
            new CreateCaseRequest { Date = ChildDay, Title = "Podřízený", ParentCaseId = otherCase.Id },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.InvalidParent), "a parent from another tenant is no parent, and nothing is filed");
            Assert.That(result.Case, Is.Null);
            Assert.That(await this.tenant.Context.Cases.CountAsync(), Is.Zero);
        }
    }

    [Test]
    public async Task ACaseWithNoParentIsFiled()
    {
        var result = await this.writer.CreateCase(
            new CreateCaseRequest { Date = ChildDay, Title = "Samostatný", ParentCaseId = null },
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.Created));

        var reloaded = await this.tenant.Context.Cases.SingleAsync(@case => @case.Id == result.Case!.CaseId);

        Assert.That(reloaded.ParentCaseId, Is.Null);
    }
}
