using EvilBrains.EvilCase.Import;

namespace EvilBrains.EvilCase.Tests.Import;

/// <summary>
/// Every fixture is synthetic — folder and file names written here, mimicking the convention and
/// nothing else. No real case name, file mark or document name enters the repository, which is public
/// (<c>docs/product/vision.md</c>, <em>Privacy</em>).
/// </summary>
public class CaseFolderParserTests
{
    [Test]
    public void FilesSharingAnOrdinalBecomeOneAct()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "12 C 148",
            Files = ["01 - Zaloba.docx", "01 - Zaloba.pdf", "02 - Vyzva soudu.pdf"],
        });

        int[] ordinals = [1, 2];
        string[] firstAct = ["01 - Zaloba.docx", "01 - Zaloba.pdf"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tree.Root.Acts.Select(act => act.Ordinal), Is.EqualTo(ordinals), "ordinals ascending");
            Assert.That(tree.Root.Acts[0].Files.Select(file => file.Name), Is.EqualTo(firstAct), "the source and the final are one act");
            Assert.That(tree.Root.Acts[0].Title, Is.EqualTo("Zaloba"), "the extension is not part of the title");
            Assert.That(tree.Problems, Is.Empty);
        }
    }

    [Test]
    public void ALetterAfterTheOrdinalMarksAnAttachment()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "case",
            Files = ["05 - Rozhodnuti.pdf", "05a - Priloha 1 - Smlouva.pdf", "05a - Attachment 2 - Faktura.pdf"],
        });

        var act = tree.Root.Acts.Single();
        string[] attachmentTitles = ["Attachment 2 - Faktura", "Priloha 1 - Smlouva"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.Files.Count(file => file.IsAttachment), Is.EqualTo(2));
            Assert.That(act.Title, Is.EqualTo("Rozhodnuti"), "the act is titled from the file that is not an attachment");
            Assert.That(
                act.Files.Where(file => file.IsAttachment).Select(file => file.Title),
                Is.EqualTo(attachmentTitles),
                "the word after the dash is title text in whatever language, never a token the parser matches");
        }
    }

    [Test]
    public void ASubFolderIsASubCaseToAnyDepth()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "root",
            Folders =
            [
                new()
                {
                    Name = "odvolani",
                    Files = ["01 - Odvolani.pdf"],
                    Folders = [new() { Name = "dovolani" }],
                },
            ],
        });

        var sub = tree.Root.SubCases.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sub.Title, Is.EqualTo("odvolani"));
            Assert.That(sub.Acts.Single().Ordinal, Is.EqualTo(1));
            Assert.That(sub.SubCases.Single().Title, Is.EqualTo("dovolani"), "depth is not capped");
        }
    }

    [Test]
    public void TheClosedMarkerIsReadThroughItsDiacriticsAndCase()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "root",
            Folders =
            [
                new() { Name = "vec A (uzavřeno)" },
                new() { Name = "vec B (UZAVRENO)" },
                new() { Name = "vec C" },
                new() { Name = "vec D (probiha)" },
            ],
        });

        string[] expectedClosed = ["vec A", "vec B"];
        string[] expectedOpen = ["vec C", "vec D (probiha)"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                tree.Root.SubCases.Where(subCase => subCase.IsClosed).Select(subCase => subCase.Title),
                Is.EqualTo(expectedClosed),
                "the marker is folded, and the title loses it");

            Assert.That(
                tree.Root.SubCases.Where(subCase => !subCase.IsClosed).Select(subCase => subCase.Title),
                Is.EqualTo(expectedOpen),
                "another parenthesis is not the marker");
        }
    }

    [Test]
    public void GeneratedSummariesAreIgnoredEntirely()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "case",
            Files = ["01 - Zaloba.pdf", "99 - Shrnuti.docx"],
        });

        int[] ordinals = [1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tree.Root.Acts.Select(act => act.Ordinal), Is.EqualTo(ordinals), "not an act");
            Assert.That(tree.Problems, Is.Empty, "and not a problem either — it is ignored, not unreadable");
        }
    }

    [Test]
    public void AnUnreadableNameIsReportedWithItsPathRatherThanSwallowed()
    {
        var tree = CaseFolderParser.Parse(new()
        {
            Name = "root",
            Files = ["poznamky.txt"],
            Folders = [new() { Name = "odvolani", Files = ["sken.pdf", "01 - Odvolani.pdf"] }],
        });

        string[] expectedPaths = ["root/poznamky.txt", "root/odvolani/sken.pdf"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                tree.Problems.Select(problem => problem.Path),
                Is.EqualTo(expectedPaths),
                "problems come from the whole tree, each with the path that locates it");

            Assert.That(tree.Root.SubCases.Single().Acts, Has.Count.EqualTo(1), "and the readable files still import");
        }
    }

    [Test]
    public void NothingIsWrittenBackIntoTheTreeItRead()
    {
        var source = new FolderNode
        {
            Name = "case (uzavřeno)",
            Files = ["01 - Zaloba.pdf", "nonsense.txt"],
        };

        _ = CaseFolderParser.Parse(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(source.Name, Is.EqualTo("case (uzavřeno)"), "the source is read-only reference material");
            Assert.That(source.Files, Has.Count.EqualTo(2));
        }
    }
}
