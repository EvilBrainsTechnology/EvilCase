using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Reads the SQL the delete guard produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ContactReferencesTests
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
    public void TheGuardAsksAllFourPlacesInOneQuery()
    {
        var id = Guid.CreateVersion7();
        var sql = this.context.Contacts
            .Where(contact => contact.Id == id)
            .Referenced()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Acts\""));
            Assert.That(sql, Does.Contain("\"ExternalCaseNumbers\""));
            Assert.That(sql, Does.Contain("\"ExternalActNumbers\""));
            Assert.That(sql, Does.Contain("EXISTS"));
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "the guard asks whether a row exists and counts nothing");
        }
    }
}
