using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
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
        var sql = this.context.ExternalCaseNumbers.AsCaseOccurrences(Guid.CreateVersion7()).ToQueryString();

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
    public void IssuedByAndAddressedToOccurrencesEachNarrowByTheirOwnColumn()
    {
        var id = Guid.CreateVersion7();
        var issuedBy = this.context.Acts.AsIssuedByOccurrences(id).ToQueryString();
        var addressedTo = this.context.Acts.AsAddressedToOccurrences(id).ToQueryString();

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
        var sql = this.context.ExternalActNumbers.AsNumberIssuerOccurrences(Guid.CreateVersion7()).ToQueryString();

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

        var sources = new string[]
        {
            this.context.ExternalCaseNumbers.AsCaseOccurrences(id).ToQueryString(),
            this.context.Acts.AsIssuedByOccurrences(id).ToQueryString(),
            this.context.Acts.AsAddressedToOccurrences(id).ToQueryString(),
            this.context.ExternalActNumbers.AsNumberIssuerOccurrences(id).ToQueryString(),
        };

        using (Assert.EnterMultipleScope())
        {
            foreach (var sql in sources)
                Assert.That(sql, Does.Contain("\"TenantId\""), "a query filter is what keeps another tenant's rows out");
        }
    }
}
