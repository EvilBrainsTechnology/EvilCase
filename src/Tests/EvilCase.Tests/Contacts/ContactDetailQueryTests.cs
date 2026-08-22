using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Reads the SQL the detail header produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ContactDetailQueryTests
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
    public void TheHeaderIsNarrowedByTheIdAndTheTenant()
    {
        var sql = this.context.Contacts
            .WithId(Guid.CreateVersion7())
            .AsDetail()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Id\" = "), "the header reads one contact by its identifier");
            Assert.That(sql, Does.Contain("\"TenantId\""), "a query filter is what keeps another tenant's contact out");
        }
    }

    [Test]
    public void TheHeaderReadsTheContactsOwnColumnsAlone()
    {
        var sql = this.context.Contacts
            .AsDetail()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Name\""));
            Assert.That(sql, Does.Contain("\"Kind\""));
            Assert.That(sql, Does.Contain("\"DataBoxId\""));
            Assert.That(sql, Does.Contain("\"Address\""));
            Assert.That(sql, Does.Not.Contain("\"Acts\""), "the occurrences are read on their own, not joined into the header");
            Assert.That(sql, Does.Not.Contain("\"Users\""), "the default-contact flag is read on its own, not joined into the header");
        }
    }
}
