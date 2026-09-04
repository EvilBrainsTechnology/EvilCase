using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActContactPairingTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    protected override bool AsHost => true;

    [Test]
    public async Task ADirectionWithNoContactIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);

        this.Tenant.Context.Acts.Add(new Act
        {
            CaseId = @case.Id,
            ActNumber = ActNumberFormat.Compose(@case.CaseNumber, Day, 1),
            Direction = ActDirection.Incoming,
            Title = "Podání",
            Date = Day,
        });

        Assert.That(
            async () => await this.Tenant.Context.SaveChangesAsync(),
            Throws.InstanceOf<DbUpdateException>(),
            "the check constraint is what keeps a direction without a contact out");
    }

    [Test]
    public async Task AContactWithNoDirectionIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Městský úřad Vzorov");

        this.Tenant.Context.Acts.Add(new Act
        {
            CaseId = @case.Id,
            ActNumber = ActNumberFormat.Compose(@case.CaseNumber, Day, 1),
            Direction = null,
            ContactId = contact.Id,
            Title = "Podání",
            Date = Day,
        });

        Assert.That(
            async () => await this.Tenant.Context.SaveChangesAsync(),
            Throws.InstanceOf<DbUpdateException>(),
            "the check constraint is what keeps a contact without a direction out");
    }
}
