using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Reads the SQL the reference check for delete really runs, without a server.
/// </summary>
public class ContactWriterTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheReferenceCheckReachesEveryPlaceAContactCanBeNamed()
    {
        var sql = ContactWriter.ReferencesTo(this.context, Guid.CreateVersion7()).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"ExternalCaseNumbers\""), "a mark the contact assigned to a case blocks the delete");
            Assert.That(sql, Does.Contain("\"IssuedByContactId\""), "an act the contact issued blocks the delete");
            Assert.That(sql, Does.Contain("\"AddressedToContactId\""), "an act addressed to the contact blocks the delete");
            Assert.That(sql, Does.Contain("\"ExternalActNumbers\""), "a mark the contact assigned to an act blocks the delete");
            Assert.That(sql, Does.Contain("UNION"), "the four sources are read as one query");
        }
    }

    [Test]
    public void TheReferenceCheckStaysInsideTheTenant()
    {
        var sql = ContactWriter.ReferencesTo(this.context, Guid.CreateVersion7()).ToQueryString();

        Assert.That(sql, Does.Contain("\"TenantId\""), "a query filter is what keeps another tenant's rows out");
    }
}
