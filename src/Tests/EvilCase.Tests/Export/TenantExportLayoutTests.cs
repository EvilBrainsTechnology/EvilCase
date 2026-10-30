using EvilBrains.EvilCase.Business.Export;

namespace EvilBrains.EvilCase.Tests.Export;

public class TenantExportLayoutTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    public void AFolderIsTheDateAndTheTitleWithoutWhatAFileNameCannotHold()
    {
        Assert.That(TenantExportLayout.FolderName(Day, "Odvolání: 1/2", TenantExportLayout.CaseFallback), Is.EqualTo("2026-08-21 Odvolání_ 1_2"));
    }

    [Test]
    public void ALongTitleIsCutToEightyCharactersWithoutATrailingSpace()
    {
        var title = new string('a', 79) + " " + new string('b', 20);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(TenantExportLayout.FolderName(Day, title, TenantExportLayout.CaseFallback), Is.EqualTo("2026-08-21 " + new string('a', 79)), "a file system drops a trailing space");
            Assert.That(TenantExportLayout.ContactDocumentName(new string('c', 100)), Is.EqualTo(new string('c', 80) + ".json"));
        }
    }

    [Test]
    public void TheCutNeverSplitsACharacterOfTwoHalves()
    {
        var title = new string('a', 79) + "\U0001F4C4";

        Assert.That(TenantExportLayout.FolderName(Day, title, TenantExportLayout.CaseFallback), Is.EqualTo("2026-08-21 " + new string('a', 79)));
    }

    [Test]
    public void ANameLeftEmptyTakesTheNameOfItsKind()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TenantExportLayout.FolderName(Day, " .. ", TenantExportLayout.CaseFallback), Is.EqualTo("2026-08-21 Spis"));
            Assert.That(TenantExportLayout.FolderName(Day, "...", TenantExportLayout.ActFallback), Is.EqualTo("2026-08-21 Úkon"));
            Assert.That(TenantExportLayout.ContactDocumentName(". "), Is.EqualTo("Kontakt.json"));
        }
    }

    [Test]
    public void TheArchiveCarriesThePragueDateOfTheExport()
    {
        var lateEvening = new DateTimeOffset(2026, 10, 30, 23, 30, 0, TimeSpan.Zero);

        Assert.That(TenantExportLayout.ArchiveName(lateEvening), Is.EqualTo("EvilCase export 2026-10-31.zip"), "half past midnight in Prague is the next day");
    }
}
