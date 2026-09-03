using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Business.Seeding;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Seeding;

/// <summary>
/// The seed against a real database, wired the way the host wires it. A fake context tracks whatever a
/// test added, which hides a seed that leans on the change tracker.
/// </summary>
public class SampleDataSeedDatabaseTests
{
    [Test]
    public async Task TheSeedFillsAFreshTenantOnARealDatabase()
    {
        var userContext = new StubUserContext();
        await using var context = TestDatabase.CreateMigratedAsHost(userContext);

        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await SeedFreshTenant(context, userContext, tenantId, userId);

        var session = new FixedDbSession(context);
        var seeder = new SampleDataSeeder(
            session,
            new CaseNumberIssuer(session),
            new ActNumberIssuer(session),
            new FakeFileBlobStore(),
            userContext,
            NullLogger<SampleDataSeeder>.Instance);

        await seeder.SeedSampleData(tenantId, userId, CancellationToken.None);

        using (userContext.Enter(tenantId, userId))
        {
            var cases = await context.Cases.ToListAsync();
            var actCount = await context.Acts.CountAsync();
            var contactCount = await context.Contacts.CountAsync();
            var fileAssetCount = await context.FileAssets.CountAsync();
            var commentCount = await context.Comments.CountAsync();
            var mainCase = cases.Single(static @case => @case.ParentCaseId is null);
            var actsWithExternalNumber = await context.Acts.CountAsync(static act => act.ExternalActNumber != null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cases, Has.Count.EqualTo(17), "the seed writes the main case and its sixteen sub-cases into a real database");
                Assert.That(cases.Count(static @case => @case.ParentCaseId is null), Is.EqualTo(1), "exactly one case has no parent");
                Assert.That(cases.Select(static @case => @case.CaseNumber).Distinct().Count(), Is.EqualTo(17), "the issuer gives every seeded case its own number");
                Assert.That(cases.TrueForAll(@case => @case.TenantId == tenantId), Is.True, "the write stamps the seeded tenant, which the seed itself never names");
                Assert.That(cases.TrueForAll(@case => @case.UserId == userId), Is.True, "the write stamps the seeding user, which the seed itself never names");
                Assert.That(actCount, Is.EqualTo(55), "23 main-case acts plus two on each of the sixteen sub-cases");
                Assert.That(contactCount, Is.EqualTo(13), "the twelve sample contacts beside the user's default contact");
                Assert.That(fileAssetCount, Is.EqualTo(57), "55 act files, one main-case file and one evidence bundle");
                Assert.That(commentCount, Is.EqualTo(6), "the six sample comments");
                Assert.That(mainCase.ExternalCaseNumber, Is.EqualTo("VV41/2025/08464"), "the main case carries its external mark");
                Assert.That(actsWithExternalNumber, Is.GreaterThan(0), "acts carry their external reference numbers");
            }
        }
    }

    private static async Task SeedFreshTenant(ApplicationDbContext context, StubUserContext userContext, Guid tenantId, Guid userId)
    {
        using (userContext.Enter(tenantId, userId))
        {
            var account = new Account { Name = "sample-data-seed" };
            context.Accounts.Add(account);
            context.Tenants.Add(new Tenant { Id = tenantId, AccountId = account.Id, Name = "sample-data-seed" });

            var defaultContact = new Contact { TenantId = tenantId, Kind = ContactKind.Person, Name = "default" };
            context.Contacts.Add(defaultContact);

            context.Users.Add(new User
            {
                Id = userId,
                TenantId = tenantId,
                Email = $"{Guid.CreateVersion7()}@example.com",
                PasswordHash = "hash",
                Role = UserRole.User,
                DefaultContactId = defaultContact.Id,
            });

            await context.SaveChangesAsync();
        }
    }
}
