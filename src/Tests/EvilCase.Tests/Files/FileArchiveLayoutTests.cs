using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Domain.Cases;

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
            Assert.That(entries.Select(static entry => entry.EntryName), Is.EqualTo(["Smlouva (version 1).pdf", "Smlouva (version 3).pdf", "Smlouva.pdf", "Poznámky (version 1)", "Poznámky"]));
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
            [Version(smlouva, "Smlouva.pdf", 1), Version(smlouva, "Smlouva.pdf", 2), Version(named, "smlouva (version 1).PDF", 1), Version(other, "Smlouva (version 1) (2).pdf", 1)],
            includeVersions: true);

        Assert.That(
            entries.Select(static entry => entry.EntryName),
            Is.EqualTo(["Smlouva (version 1) (3).pdf", "Smlouva.pdf", "smlouva (version 1).PDF", "Smlouva (version 1) (2).pdf"]),
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
    [TestCase(" .. ", "file")]
    [TestCase("faktura‮fdp.exe", "faktura_fdp.exe")]
    public void AnEntryNameCarriesNoPathStep(string fileName, string expected)
    {
        var entries = FileArchiveLayout.Entries([Version(Guid.NewGuid(), fileName, 1)], includeVersions: false);

        Assert.That(entries.Single().EntryName, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("Přestupek Novák", false, "Přestupek Novák.zip")]
    [TestCase("Přestupek Novák", true, "Přestupek Novák (with versions).zip")]
    [TestCase("Odvolání: 1/2026?", false, "Odvolání_ 1_2026_.zip")]
    [TestCase(" ?. ", false, "_.zip")]
    [TestCase(" . ", true, "act files (with versions).zip")]
    public void TheArchiveIsNamedAfterItsAct(string title, bool includeVersions, string expected)
    {
        Assert.That(FileArchiveLayout.ArchiveName(title, FileArchiveLayout.ActArchiveFallback, includeVersions), Is.EqualTo(expected));
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

    [Test]
    public void ACaseFolderWithoutFilesClaimsNoNameBesideItsNamesakes()
    {
        var day = new DateOnly(2026, 9, 2);
        var smlouva = Guid.NewGuid();
        var navrh = Guid.NewGuid();
        var zprava = Guid.NewGuid();
        var tree = CaseNode(
            day,
            [CaseNode(day, [], [ActNode(day, "Podání", [])]), CaseNode(day, [], [ActNode(day, "Podání", [smlouva])])],
            [ActNode(day, "Podání", []), ActNode(day, "Podání", [navrh]), ActNode(day, "podání", [zprava])]);

        var entries = FileArchiveLayout.CaseEntries(tree, [Version(smlouva, "Smlouva.pdf", 1), Version(navrh, "Návrh.docx", 1), Version(zprava, "Zpráva.pdf", 1)], includeVersions: false);

        Assert.That(
            entries.Select(static entry => entry.EntryName),
            Is.EqualTo(["2026-09-02 Spis/2026-09-02 Podání/Smlouva.pdf", "2026-09-02 Podání/Návrh.docx", "2026-09-02 podání (2)/Zpráva.pdf"]),
            "a folder without a file is not in the archive, so its name stays free; a taken name, ignoring case, gets the next free number");
    }

    private static CaseFileTree CaseNode(DateOnly date, IReadOnlyList<CaseFileTree> subCases, IReadOnlyList<ActFileTree> acts)
    {
        return new CaseFileTree
        {
            Case = new CaseListItem
            {
                CaseId = Guid.NewGuid(),
                CaseNumber = "1",
                Title = "Spis",
                Date = date,
                Status = CaseStatus.Active,
                Changed = Moment,
            },
            SubCases = subCases,
            Acts = acts,
        };
    }

    private static ActFileTree ActNode(DateOnly date, string title, IReadOnlyList<Guid> fileIds)
    {
        return new ActFileTree
        {
            Act = new ActListItem
            {
                ActId = Guid.NewGuid(),
                CaseId = Guid.NewGuid(),
                CaseNumber = "1",
                CaseTitle = "Spis",
                ActNumber = "1",
                Title = title,
                Date = date,
                Changed = Moment,
            },
            Files =
            [
                .. fileIds.Select(static fileId => new FileListItem
                {
                    FileId = fileId,
                    FileName = "",
                    SizeBytes = 1,
                    Created = Moment,
                    Version = 1,
                    VersionCount = 1,
                }),
            ],
        };
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
