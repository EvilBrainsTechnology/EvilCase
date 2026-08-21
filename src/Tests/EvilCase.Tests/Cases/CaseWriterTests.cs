using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Tenancy;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseWriterTests
{
    [Test]
    public void ANewCaseIsActiveAndHangsUnderNothing()
    {
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = null };

        var @case = CaseWriter.Build(request, "1/2026", new FakeTenantContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.Status, Is.EqualTo(CaseStatus.Active));
            Assert.That(@case.ParentCaseId, Is.Null);
            Assert.That(@case.CaseNumber, Is.EqualTo("1/2026"));
            Assert.That(@case.Date, Is.EqualTo(request.Date));
            Assert.That(@case.Title, Is.EqualTo(request.Title));
        }
    }

    [Test]
    public void ABlankDescriptionIsFiledAsNothing()
    {
        var blank = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "   " };
        var withText = blank with { Description = "text" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseWriter.Build(blank, "1/2026", new FakeTenantContext()).Description, Is.Null);
            Assert.That(CaseWriter.Build(withText, "1/2026", new FakeTenantContext()).Description, Is.EqualTo("text"));
        }
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public Guid TenantId { get; } = Guid.CreateVersion7();

        public Guid? TenantIdOrDefault => this.TenantId;

        public Guid UserId { get; } = Guid.CreateVersion7();

        public IDisposable Enter(Guid tenantId) => throw new NotSupportedException();
    }
}
