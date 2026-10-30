using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Business.Files.Links;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;
using EvilBrains.EvilCase.Domain.Files;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileLinkWriterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 10, 30);

    private readonly TestTimeProvider time = new(new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc));

    private ApiToken apiToken = null!;

    private FileLinkWriter writer = null!;

    [SetUp]
    public async Task SetUpWriter()
    {
        this.apiToken = new ApiToken
        {
            UserId = this.Tenant.UserId,
            Name = "agent",
            Scope = ApiTokenScope.ReadWrite,
            TokenHash = ApiTokenValue.Hash(ApiTokenValue.Create()),
            Expires = this.time.UtcNow.AddDays(90),
        };

        this.Tenant.Context.ApiTokens.Add(this.apiToken);
        await this.Tenant.Context.SaveChangesAsync();

        this.writer = this.WriterOn(this.Tenant.Context);
    }

    [Test]
    public async Task ALinkIsSpentByItsFirstUse()
    {
        var @case = await this.Tenant.AddCase(Day);
        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);

        await using var anonymous = TestDatabase.CreateMigrated(new StubUserContext());
        var anonymousWriter = this.WriterOn(anonymous);

        var first = await anonymousWriter.ConsumeFileLink(created.Secret!, CancellationToken.None);
        var second = await anonymousWriter.ConsumeFileLink(created.Secret!, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Outcome, Is.EqualTo(FileLinkCreateOutcome.Created));
            Assert.That(created.Expires, Is.EqualTo(this.time.UtcNow.AddMinutes(10)), "a link is valid ten minutes");
            Assert.That(first?.TenantId, Is.EqualTo(this.Tenant.UserContext.TenantId), "the use runs in the tenant that created the link, found before any tenant is known");
            Assert.That(first?.UserId, Is.EqualTo(this.Tenant.UserId), "the use runs as the user that created the link");
            Assert.That(first?.Kind, Is.EqualTo(FileLinkKind.CaseUpload));
            Assert.That(first?.TargetId, Is.EqualTo(@case.Id));
            Assert.That(second, Is.Null, "a link is spent by its first use");
        }
    }

    [Test]
    public async Task TwoUsesAtOnceSpendTheLinkOnce()
    {
        var @case = await this.Tenant.AddCase(Day);
        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);

        await using var one = TestDatabase.CreateMigrated(new StubUserContext());
        await using var two = TestDatabase.CreateMigrated(new StubUserContext());

        var uses = await Task.WhenAll(
            this.WriterOn(one).ConsumeFileLink(created.Secret!, CancellationToken.None),
            this.WriterOn(two).ConsumeFileLink(created.Secret!, CancellationToken.None));

        Assert.That(uses.Count(static use => use is not null), Is.EqualTo(1), "one statement finds and deletes the link, so only one of two concurrent uses gets it");
    }

    [Test]
    public async Task AnExpiredLinkIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);

        this.time.Advance(TimeSpan.FromMinutes(10));
        var used = await this.writer.ConsumeFileLink(created.Secret!, CancellationToken.None);

        Assert.That(used, Is.Null, "a link is never usable once expired, removed or not");
    }

    [Test]
    public async Task TheCleanupRemovesOnlyExpiredLinks()
    {
        var @case = await this.Tenant.AddCase(Day);
        var older = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);
        this.time.Advance(TimeSpan.FromMinutes(6));
        var newer = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);
        this.time.Advance(TimeSpan.FromMinutes(5));

        await this.writer.DeleteExpiredFileLinks(CancellationToken.None);

        var remaining = await this.Tenant.Context.FileLinks
            .AsNoTracking()
            .Where(link => link.TargetId == @case.Id)
            .Select(static link => link.Expires)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(older.Expires, Is.LessThan(this.time.UtcNow));
            Assert.That(remaining, Is.EqualTo([newer.Expires]), "the cleanup removes the expired link and keeps the live one");
        }
    }

    [Test]
    public async Task ALinkToACaseOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);

        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, otherCase.Id, version: null, this.apiToken.Id, CancellationToken.None);
        var stored = await this.Tenant.Context.FileLinks.CountAsync(link => link.TargetId == otherCase.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Outcome, Is.EqualTo(FileLinkCreateOutcome.TargetNotFound), "a link reaches only a target of the caller's tenant");
            Assert.That(stored, Is.Zero, "a refused link writes no row");
        }
    }

    [Test]
    public async Task ADownloadLinkToAnUnknownVersionIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case);

        var created = await this.writer.CreateFileLink(FileLinkKind.Download, file.Id, version: 2, this.apiToken.Id, CancellationToken.None);

        Assert.That(created.Outcome, Is.EqualTo(FileLinkCreateOutcome.TargetNotFound));
    }

    [Test]
    public async Task TheRowHoldsOnlyTheHashOfTheSecret()
    {
        var @case = await this.Tenant.AddCase(Day);
        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);

        var stored = await this.Tenant.Context.FileLinks
            .AsNoTracking()
            .SingleAsync(link => link.TargetId == @case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Secret, Has.Length.EqualTo(43), "the secret is 256 random bits in base64url");
            Assert.That(stored.SecretHash, Is.Not.EqualTo(created.Secret).And.Length.EqualTo(64), "the row stores the SHA-256 of the secret, never the secret");
        }
    }

    [Test]
    public async Task RevokingTheTokenTakesItsLinksAlong()
    {
        var @case = await this.Tenant.AddCase(Day);
        var created = await this.writer.CreateFileLink(FileLinkKind.CaseUpload, @case.Id, version: null, this.apiToken.Id, CancellationToken.None);

        await this.Tenant.Context.ApiTokens
            .Where(stored => stored.Id == this.apiToken.Id)
            .ExecuteDeleteAsync();

        var used = await this.writer.ConsumeFileLink(created.Secret!, CancellationToken.None);

        Assert.That(used, Is.Null, "a revoked token's links stop working with it");
    }

    private FileLinkWriter WriterOn(ApplicationDbContext context)
    {
        return new FileLinkWriter(new FixedDbSession(context), this.Tenant.UserContext, this.time, NullLogger<FileLinkWriter>.Instance);
    }
}
