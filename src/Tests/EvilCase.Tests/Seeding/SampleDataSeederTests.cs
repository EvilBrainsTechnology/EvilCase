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
    [Test]
    public async Task TheSeedFillsTheTenantWithTheWholeCaseTree()
    {
        var (context, _, userId) = await Run();

        var cases = context.Added<Case>().ToList();
        var caseIds = cases.Select(@case => @case.Id).ToHashSet();
        var roots = cases.Where(@case => @case.ParentCaseId is null).ToList();

        var grandchildren = cases.Count(@case => @case.ParentCaseId is not null && cases.Single(parent => parent.Id == @case.ParentCaseId).ParentCaseId is not null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cases, Has.Count.EqualTo(17), "the seed writes the main case and its sixteen sub-cases");
            Assert.That(roots, Has.Count.EqualTo(1), "exactly one case has no parent");
            Assert.That(cases.Except(roots).All(@case => caseIds.Contains(@case.ParentCaseId!.Value)), Is.True, "every other case's parent is a seeded case");
            Assert.That(grandchildren, Is.EqualTo(1), "exactly one case's parent is itself a sub-case, giving the tree three levels");
            Assert.That(cases.TrueForAll(@case => !string.IsNullOrEmpty(@case.CaseNumber)), Is.True, "every case carries a number");
            Assert.That(cases.TrueForAll(@case => @case.UserId == userId), Is.True, "every case belongs to the seeding user");
        }
    }

    [Test]
    public async Task EveryActBelongsToASeededCaseAndNamesItsSender()
    {
        var (context, _, userId) = await Run();

        var caseIds = context.Added<Case>().Select(@case => @case.Id).ToHashSet();
        var contactIds = context.Added<Contact>().Select(contact => contact.Id).ToHashSet();
        var acts = context.Added<Act>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acts, Has.Count.EqualTo(55), "23 main-case acts plus two generated acts on each of the sixteen sub-cases");
            Assert.That(acts.TrueForAll(act => caseIds.Contains(act.CaseId)), Is.True, "every act hangs on a seeded case");
            Assert.That(acts.TrueForAll(act => act.IssuedByContactId != Guid.Empty && contactIds.Contains(act.IssuedByContactId)), Is.True, "every act names a seeded issuer");
            Assert.That(acts.TrueForAll(act => act.AddressedToContactId is not null && act.AddressedToContactId != Guid.Empty && contactIds.Contains(act.AddressedToContactId.Value)), Is.True, "every act names a seeded recipient");
            Assert.That(acts.TrueForAll(act => !string.IsNullOrEmpty(act.ActNumber)), Is.True, "every act carries a number");
            Assert.That(acts.TrueForAll(act => act.UserId == userId), Is.True, "every act belongs to the seeding user");
        }
    }

    [Test]
    public async Task TheActsOfEveryCaseRunInDateOrder()
    {
        var (context, _, _) = await Run();

        var groups = context.Added<Act>()
            .GroupBy(act => act.CaseId)
            .Select(group => group.Select(act => act.Date).ToList())
            .ToList();

        Assert.That(groups.TrueForAll(dates => dates.Zip(dates.Skip(1)).All(pair => pair.Second >= pair.First)), Is.True, "an act list never goes backwards in date");
    }

    [Test]
    public async Task EveryFileHasABlobAndExactlyOneOwner()
    {
        var (context, blobs, _) = await Run();

        var assets = context.Added<FileAsset>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(assets, Has.Count.EqualTo(57), "55 act files, one main-case file and one evidence bundle");
            Assert.That(blobs.Written, Has.Count.EqualTo(57), "every asset row is backed by exactly one written blob");
            Assert.That(assets.TrueForAll(asset => blobs.Written.ContainsKey(asset.Id)), Is.True, "every asset id has a blob");
            Assert.That(assets.TrueForAll(asset => asset.SizeBytes > 0), Is.True, "every blob has content");
            Assert.That(assets.TrueForAll(asset => asset.ContentHash.Length == 64), Is.True, "every asset carries a SHA-256 hash");
            Assert.That(assets.TrueForAll(asset => !string.IsNullOrEmpty(asset.StoragePath)), Is.True, "every asset knows where it landed");
            Assert.That(assets.TrueForAll(asset => string.Equals(asset.MediaType, "text/plain", StringComparison.Ordinal)), Is.True, "every seeded file is plain text");
            Assert.That(assets.TrueForAll(asset => asset.FileName.EndsWith(".txt", StringComparison.Ordinal)), Is.True, "every seeded file name ends in .txt");
            Assert.That(assets.TrueForAll(asset => (asset.CaseId is null) != (asset.ActId is null)), Is.True, "every file belongs to exactly one case or act");
        }
    }

    [Test]
    public async Task ExternalNumbersNameTheContactThatAssignedThem()
    {
        var (context, _, _) = await Run();

        var contactIds = context.Added<Contact>().Select(contact => contact.Id).ToHashSet();
        var caseNumbers = context.Added<ExternalCaseNumber>().ToList();
        var actNumbers = context.Added<ExternalActNumber>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseNumbers, Is.Not.Empty, "the main case carries external numbers");
            Assert.That(actNumbers, Is.Not.Empty, "acts carry external numbers");
            Assert.That(caseNumbers.TrueForAll(number => contactIds.Contains(number.AssignedByContactId)), Is.True, "every case-level number names a seeded contact");
            Assert.That(actNumbers.TrueForAll(number => contactIds.Contains(number.AssignedByContactId)), Is.True, "every act-level number names a seeded contact");
            Assert.That(caseNumbers.Select(number => number.Value), Does.Contain("VV41/2025/08464"));
            Assert.That(caseNumbers.Select(number => number.Value), Does.Contain("10 A 1/2025"));
            Assert.That(actNumbers.Select(number => number.Value), Does.Contain("MUVZ/2025/80535"));
            Assert.That(actNumbers.Select(number => number.Value), Does.Contain("KUVZ 109838/2025"));
        }
    }

    [Test]
    public async Task TheSeededContactsAreTheOnesTheCaseNames()
    {
        var (context, _, _) = await Run();

        var contacts = context.Added<Contact>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contacts, Has.Count.EqualTo(12), "the case names twelve contacts");
            Assert.That(contacts.Select(contact => contact.Name), Does.Contain("Ing. Petr Vzorek"));
            Assert.That(contacts.Select(contact => contact.Name), Does.Contain("Městský úřad Vzorov, odbor vnitřních věcí"));
            Assert.That(contacts.Select(contact => contact.Name), Does.Contain("Krajský soud ve Vzorově"));
            Assert.That(contacts.Count(contact => contact.Kind == ContactKind.Person), Is.EqualTo(1), "the subject is the only person");
            Assert.That(contacts.Count(contact => contact.Kind == ContactKind.Official), Is.EqualTo(2), "the mayor and the officer are the only officials");
        }
    }

    [Test]
    public async Task EveryCommentHangsOnACaseOrAnAct()
    {
        var (context, _, userId) = await Run();

        var comments = context.Added<Comment>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comments, Has.Count.EqualTo(6));
            Assert.That(comments.TrueForAll(comment => (comment.CaseId is null) != (comment.ActId is null)), Is.True, "every comment hangs on exactly one case or act");
            Assert.That(comments.TrueForAll(comment => !string.IsNullOrEmpty(comment.Body)), Is.True, "every comment carries a body");
            Assert.That(comments.TrueForAll(comment => comment.UserId == userId), Is.True, "every comment belongs to the seeding user");
        }
    }

    private static async Task<(FakeApplicationDbContext Context, FakeFileBlobStore Blobs, Guid UserId)> Run()
    {
        var tenantContext = new StubTenantContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        using var scope = tenantContext.Enter(tenantId);

        var context = FakeApplicationDbContext.Create(tenantContext);
        var blobs = new FakeFileBlobStore();

        var seeder = new SampleDataSeeder(
            new FixedDbSession(context),
            new FakeCaseNumberIssuer(),
            new FakeActNumberIssuer(),
            blobs,
            NullLogger<SampleDataSeeder>.Instance);

        await seeder.Seed(tenantId, userId, CancellationToken.None);

        return (context, blobs, userId);
    }
}
