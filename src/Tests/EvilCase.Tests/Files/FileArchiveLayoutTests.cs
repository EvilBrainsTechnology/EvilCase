using EvilBrains.EvilCase.Business.Files;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileArchiveLayoutTests
{
    private static readonly DateTime Moment = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void TheCurrentVersionOfEachFileGoesUnderItsNameInFileOrder()
    {
        var smlouva = Guid.NewGuid();
        var navrh = Guid.NewGuid();

        var entries = FileArchiveLayout.Entries([Version(smlouva, "Smlouva.pdf", 1), Version(smlouva, "Smlouva.pdf", 2), Version(navrh, "Návrh.docx", 1)], includeVersions: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Select(static entry => entry.EntryName), Is.EqualTo(["Smlouva.pdf", "Návrh.docx"]));
            Assert.That(entries[0].StoragePath, Is.EqualTo(StoragePathOf(smlouva, 2)), "the current version is the highest number");
            Assert.That(entries[0].Created, Is.EqualTo(new DateTimeOffset(Moment.AddDays(2))), "an entry carries the upload moment of its version");
        }
    }

    [Test]
    public void AllVersionsPutTheOlderOnesBesideTheFileByNumber()
    {
        var smlouva = Guid.NewGuid();
        var poznamky = Guid.NewGuid();

        var entries = FileArchiveLayout.Entries(
            [Version(smlouva, "Smlouva.pdf", 1), Version(smlouva, "Smlouva.pdf", 3), Version(smlouva, "Smlouva.pdf", 4), Version(poznamky, "Poznámky", 1), Version(poznamky, "Poznámky", 2)],
            includeVersions: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Select(static entry => entry.EntryName), Is.EqualTo(["Smlouva (verze 1).pdf", "Smlouva (verze 3).pdf", "Smlouva.pdf", "Poznámky (verze 1)", "Poznámky"]));
            Assert.That(entries.Select(static entry => entry.StoragePath), Is.EqualTo([StoragePathOf(smlouva, 1), StoragePathOf(smlouva, 3), StoragePathOf(smlouva, 4), StoragePathOf(poznamky, 1), StoragePathOf(poznamky, 2)]));
        }
    }

    [Test]
    public void AFileNamedLikeAVersionKeepsItsNameAndTheVersionTakesTheNextFree()
    {
        var smlouva = Guid.NewGuid();
        var named = Guid.NewGuid();
        var other = Guid.NewGuid();

        var entries = FileArchiveLayout.Entries(
            [Version(smlouva, "Smlouva.pdf", 1), Version(smlouva, "Smlouva.pdf", 2), Version(named, "smlouva (verze 1).PDF", 1), Version(other, "Smlouva (verze 1) (2).pdf", 1)],
            includeVersions: true);

        Assert.That(
            entries.Select(static entry => entry.EntryName),
            Is.EqualTo(["Smlouva (verze 1) (3).pdf", "Smlouva.pdf", "smlouva (verze 1).PDF", "Smlouva (verze 1) (2).pdf"]),
            "real files claim their names first; a taken name, ignoring case, gets the next free number");
    }

    [Test]
    public void NamesThatMeetOnceMadeSafeTakeTheNextFreeNumberInFileOrder()
    {
        var entries = FileArchiveLayout.Entries([Version(Guid.NewGuid(), "a?.pdf", 1), Version(Guid.NewGuid(), "a_.pdf", 1), Version(Guid.NewGuid(), "a*.pdf", 1)], includeVersions: false);

        Assert.That(entries.Select(static entry => entry.EntryName), Is.EqualTo(["a_.pdf", "a_ (2).pdf", "a_ (3).pdf"]));
    }

    [Test]
    [TestCase("..\\..\\evil.txt", "_._evil.txt")]
    [TestCase("a/b.pdf", "a_b.pdf")]
    [TestCase("Zpráva...pdf", "Zpráva.pdf")]
    [TestCase(" .. ", "soubor")]
    [TestCase("faktura‮fdp.exe", "faktura_fdp.exe")]
    public void AnEntryNameCarriesNoPathStep(string fileName, string expected)
    {
        var entries = FileArchiveLayout.Entries([Version(Guid.NewGuid(), fileName, 1)], includeVersions: false);

        Assert.That(entries.Single().EntryName, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("Přestupek Novák", false, "Přestupek Novák.zip")]
    [TestCase("Přestupek Novák", true, "Přestupek Novák (včetně verzí).zip")]
    [TestCase("Odvolání: 1/2026?", false, "Odvolání_ 1_2026_.zip")]
    [TestCase(" ?. ", false, "_.zip")]
    [TestCase(" . ", true, "Soubory spisu (včetně verzí).zip")]
    public void TheArchiveIsNamedAfterItsOwner(string title, bool includeVersions, string expected)
    {
        Assert.That(FileArchiveLayout.ArchiveName(title, "Soubory spisu", includeVersions), Is.EqualTo(expected));
    }

    [Test]
    public void AnEntryCarriesItsUploadInPragueTimeForItsOwnMoment()
    {
        var smlouva = Guid.NewGuid();
        var summer = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);
        var winter = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);

        var entries = FileArchiveLayout.Entries(
            [Version(smlouva, "Smlouva.pdf", 1) with { Created = summer }, Version(smlouva, "Smlouva.pdf", 2) with { Created = winter }],
            includeVersions: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries[0].Created, Is.EqualTo(new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.FromHours(2))).WithSameOffset, "summer time is two hours ahead of UTC");
            Assert.That(entries[1].Created, Is.EqualTo(new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.FromHours(1))).WithSameOffset, "winter time is one hour ahead of UTC");
        }
    }

    [Test]
    public void AnOwnerWithoutFilesHasNoEntries()
    {
        Assert.That(FileArchiveLayout.Entries([], includeVersions: true), Is.Empty);
    }

    private static ArchivedVersion Version(Guid fileId, string fileName, int number)
    {
        return new ArchivedVersion
        {
            FileId = fileId,
            FileName = fileName,
            Number = number,
            Created = Moment.AddDays(number),
            StoragePath = StoragePathOf(fileId, number),
        };
    }

    private static string StoragePathOf(Guid fileId, int number)
    {
        return $"{fileId}/{number.ToString(CultureInfo.InvariantCulture)}";
    }
}
