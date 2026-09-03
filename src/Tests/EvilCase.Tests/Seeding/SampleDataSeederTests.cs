using EvilBrains.EvilCase.Business.Seeding;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Seeding;

/// <summary>
/// The sample case tree is the one populated dataset most manual and screenshot checks run against, so
/// its shape has to hold: a real tree, real numbers, every act naming a real sender and recipient.
/// </summary>
public class SampleDataSeederTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();

    private static readonly Guid UserId = Guid.CreateVersion7();

    [Test]
    public async Task TheSeedFillsTheTenantWithTheWholeCaseTree()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var cases = context.Added<Case>().ToList();
        var caseIds = cases.Select(static @case => @case.Id).ToHashSet();
        var roots = cases.Where(static @case => @case.ParentCaseId is null).ToList();

        var grandchildren = cases.Count(@case => @case.ParentCaseId is not null && cases.Single(parent => parent.Id == @case.ParentCaseId).ParentCaseId is not null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cases, Has.Count.EqualTo(17), "the seed writes the main case and its sixteen sub-cases");
            Assert.That(roots, Has.Count.EqualTo(1), "exactly one case has no parent");
            Assert.That(cases.Except(roots).All(@case => caseIds.Contains(@case.ParentCaseId!.Value)), Is.True, "every other case's parent is a seeded case");
            Assert.That(grandchildren, Is.EqualTo(1), "exactly one case's parent is itself a sub-case, giving the tree three levels");
            Assert.That(cases.TrueForAll(static @case => !string.IsNullOrEmpty(@case.CaseNumber)), Is.True, "every case carries a number");
        }
    }

    [Test]
    public async Task EveryActBelongsToASeededCaseAndNamesItsSender()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var caseIds = context.Added<Case>().Select(static @case => @case.Id).ToHashSet();
        var contactIds = context.Added<Contact>().Select(static contact => contact.Id).ToHashSet();
        var acts = context.Added<Act>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acts, Has.Count.EqualTo(55), "23 main-case acts plus two generated acts on each of the sixteen sub-cases");
            Assert.That(acts.TrueForAll(act => caseIds.Contains(act.CaseId)), Is.True, "every act hangs on a seeded case");
            Assert.That(acts.TrueForAll(act => act.IssuedByContactId != Guid.Empty && contactIds.Contains(act.IssuedByContactId)), Is.True, "every act names a seeded issuer");
            Assert.That(acts.TrueForAll(act => act.AddressedToContactId is not null && act.AddressedToContactId != Guid.Empty && contactIds.Contains(act.AddressedToContactId.Value)), Is.True, "every act names a seeded recipient");
            Assert.That(acts.TrueForAll(static act => !string.IsNullOrEmpty(act.ActNumber)), Is.True, "every act carries a number");
        }
    }

    [Test]
    public async Task TheActsOfEveryCaseRunInDateOrder()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var groups = context.Added<Act>()
            .GroupBy(static act => act.CaseId)
            .Select(static group => group.Select(static act => act.Date).ToList())
            .ToList();

        Assert.That(groups.TrueForAll(static dates => dates.Zip(dates.Skip(1)).All(static pair => pair.Second >= pair.First)), Is.True, "an act list never goes backwards in date");
    }

    [Test]
    public async Task EveryFileHasABlobAndExactlyOneOwner()
    {
        var (context, blobs, _, _) = await RunSampleDataSeeder();

        var assets = context.Added<FileAsset>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(assets, Has.Count.EqualTo(57), "55 act files, one main-case file and one evidence bundle");
            Assert.That(blobs.Written, Has.Count.EqualTo(57), "every asset row is backed by exactly one written blob");
            Assert.That(assets.TrueForAll(asset => blobs.Written.ContainsKey(asset.Id)), Is.True, "every asset id has a blob");
            Assert.That(assets.TrueForAll(static asset => asset.SizeBytes > 0), Is.True, "every blob has content");
            Assert.That(assets.TrueForAll(static asset => asset.ContentHash.Length == 64), Is.True, "every asset carries a SHA-256 hash");
            Assert.That(assets.TrueForAll(static asset => !string.IsNullOrEmpty(asset.StoragePath)), Is.True, "every asset knows where it landed");
            Assert.That(assets.TrueForAll(static asset => string.Equals(asset.MediaType, "text/plain", StringComparison.Ordinal)), Is.True, "every seeded file is plain text");
            Assert.That(assets.TrueForAll(static asset => asset.FileName.EndsWith(".txt", StringComparison.Ordinal)), Is.True, "every seeded file name ends in .txt");
            Assert.That(assets.TrueForAll(static asset => (asset.CaseId is null) != (asset.ActId is null)), Is.True, "every file belongs to exactly one case or act");
        }
    }

    [Test]
    public async Task TheMainCaseAndItsActsCarryTheirExternalNumbers()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var mainCase = context.Added<Case>().Single(static @case => @case.ParentCaseId is null);
        var actNumbers = context.Added<Act>()
            .Select(static act => act.ExternalActNumber)
            .Where(static number => number is not null)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mainCase.ExternalCaseNumber, Is.EqualTo("VV41/2025/08464"), "the main case carries the mark the first-instance authority gave it");
            Assert.That(actNumbers, Does.Contain("MUVZ/2025/80535"));
            Assert.That(actNumbers, Does.Contain("KUVZ 109838/2025"));
            Assert.That(actNumbers, Does.Contain("10 A 1/2025"));
        }
    }

    [Test]
    public async Task TheSeededContactsAreTheOnesTheCaseNames()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var contacts = context.Added<Contact>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contacts, Has.Count.EqualTo(12), "the case names twelve contacts");
            Assert.That(contacts.Select(static contact => contact.Name), Does.Contain("Ing. Petr Vzorek"));
            Assert.That(contacts.Select(static contact => contact.Name), Does.Contain("Městský úřad Vzorov, odbor vnitřních věcí"));
            Assert.That(contacts.Select(static contact => contact.Name), Does.Contain("Krajský soud ve Vzorově"));
            Assert.That(contacts.Count(static contact => contact.Kind == ContactKind.Person), Is.EqualTo(1), "the subject is the only person");
            Assert.That(contacts.Count(static contact => contact.Kind == ContactKind.Official), Is.EqualTo(2), "the mayor and the officer are the only officials");
        }
    }

    [Test]
    public async Task NoSeededRowNamesItsOwner()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var cases = context.Added<Case>().ToList();
        var acts = context.Added<Act>().ToList();
        var comments = context.Added<Comment>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cases.TrueForAll(static @case => @case.UserId == Guid.Empty), Is.True, "the seed names no user; the write is what stamps it");
            Assert.That(acts.TrueForAll(static act => act.UserId == Guid.Empty), Is.True, "the seed names no user; the write is what stamps it");
            Assert.That(comments.TrueForAll(static comment => comment.UserId == Guid.Empty), Is.True, "the seed names no user; the write is what stamps it");
        }
    }

    [Test]
    public async Task EveryCommentHangsOnACaseOrAnAct()
    {
        var (context, _, _, _) = await RunSampleDataSeeder();

        var comments = context.Added<Comment>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comments, Has.Count.EqualTo(6));
            Assert.That(comments.TrueForAll(static comment => (comment.CaseId is null) != (comment.ActId is null)), Is.True, "every comment hangs on exactly one case or act");
            Assert.That(comments.TrueForAll(static comment => !string.IsNullOrEmpty(comment.Body)), Is.True, "every comment carries a body");
        }
    }

    [Test]
    public async Task TheSeedEntersItsUserAndCommitsItsOwnTransaction()
    {
        var (_, _, userContext, session) = await RunSampleDataSeeder();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(userContext.Entered, Is.EqualTo([(TenantId, UserId)]), "the seed names its tenant and its user together, never one alone");
            Assert.That(session.Transaction!.Committed, Is.True, "the seed commits the transaction it opened");
        }
    }

    private static async Task<(FakeApplicationDbContext Context, FakeFileBlobStore Blobs, StubUserContext UserContext, FixedDbSession Session)> RunSampleDataSeeder()
    {
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);
        var blobs = new FakeFileBlobStore();
        var session = new FixedDbSession(context);

        var seeder = new SampleDataSeeder(
            session,
            new FakeCaseNumberIssuer(),
            new FakeActNumberIssuer(),
            blobs,
            userContext,
            NullLogger<SampleDataSeeder>.Instance);

        await seeder.SeedSampleData(TenantId, UserId, CancellationToken.None);

        return (context, blobs, userContext, session);
    }
}
