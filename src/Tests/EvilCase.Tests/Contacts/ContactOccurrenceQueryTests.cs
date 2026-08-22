using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Reads the SQL each occurrence source produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ContactOccurrenceQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp()
    {
        this.context = new ApplicationDbContextFactory().CreateDbContext([]);
    }

    [TearDown]
    public void TearDown()
    {
        this.context.Dispose();
    }

    [Test]
    public void TheCaseOccurrencesReachTheCaseThroughTheExternalNumber()
    {
        var sql = this.context.ExternalCaseNumbers
            .AssignedByContact(Guid.CreateVersion7())
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"ExternalCaseNumbers\""));
            Assert.That(sql, Does.Contain("\"Cases\""));
            Assert.That(sql, Does.Contain("\"AssignedByContactId\""));
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "an occurrence stands for one mark and counts nothing under it");
        }
    }

    [Test]
    public void TheCaseOccurrenceOrderReadsTheNumbersLengthBeforeItsText()
    {
        var sql = this.context.ExternalCaseNumbers
            .AssignedByContact(Guid.CreateVersion7())
            .InCaseOccurrenceOrder()
            .ToQueryString();

        Assert.That(sql, Does.Contain("length(").IgnoreCase, "a sequence that grew a digit follows the one below it instead of preceding it");
    }

    [Test]
    public void IssuedByAndAddressedToOccurrencesEachNarrowByTheirOwnColumn()
    {
        var id = Guid.CreateVersion7();
        var issuedBy = this.context.Acts
            .IssuedByContact(id)
            .AsIssuedByOccurrences()
            .ToQueryString();

        var addressedTo = this.context.Acts
            .AddressedToContact(id)
            .AsAddressedToOccurrences()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(issuedBy, Does.Contain("\"IssuedByContactId\""));
            Assert.That(issuedBy, Does.Not.Contain("\"AddressedToContactId\""), "the issuer source never narrows by the addressee column");
            Assert.That(addressedTo, Does.Contain("\"AddressedToContactId\""));
            Assert.That(addressedTo, Does.Not.Contain("\"IssuedByContactId\""), "the addressee source never narrows by the issuer column");
        }
    }

    [Test]
    public void TheIssuerOccurrenceCarriesTheNumberAndNamesItsCase()
    {
        var sql = this.context.ExternalActNumbers
            .AssignedByContact(Guid.CreateVersion7())
            .AsNumberIssuerOccurrences()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"ExternalActNumbers\""));
            Assert.That(sql, Does.Contain("\"Acts\""));
            Assert.That(sql, Does.Contain("\"Cases\""));
            Assert.That(sql, Does.Contain("\"Value\""));
        }
    }

    [Test]
    public void EveryOccurrenceQueryStaysInsideTheTenant()
    {
        var id = Guid.CreateVersion7();

        var caseOccurrences = this.context.ExternalCaseNumbers
            .AssignedByContact(id)
            .AsCaseOccurrences()
            .ToQueryString();

        var issuedBy = this.context.Acts
            .IssuedByContact(id)
            .AsIssuedByOccurrences()
            .ToQueryString();

        var addressedTo = this.context.Acts
            .AddressedToContact(id)
            .AsAddressedToOccurrences()
            .ToQueryString();

        var numberIssuer = this.context.ExternalActNumbers
            .AssignedByContact(id)
            .AsNumberIssuerOccurrences()
            .ToQueryString();

        var sources = new string[] { caseOccurrences, issuedBy, addressedTo, numberIssuer };

        using (Assert.EnterMultipleScope())
        {
            foreach (var sql in sources)
                Assert.That(sql, Does.Contain("\"TenantId\""), "a query filter is what keeps another tenant's rows out");
        }
    }

    [Test]
    public void TheDefaultContactCheckNamesTheTenantItself()
    {
        var userContext = new StubUserContext();
        using var _ = userContext.Enter(Guid.CreateVersion7(), Guid.CreateVersion7());

        var sql = this.context.Users
            .WithDefaultContact(Guid.CreateVersion7())
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"TenantId\""), "User carries no tenant query filter, so the read names the tenant in its own predicate");
            Assert.That(sql, Does.Contain("\"DefaultContactId\""));
        }
    }
}
