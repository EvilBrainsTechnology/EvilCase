using System.Text.Json;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TextSearchLibraryTests
{
    // Prints the matches of the query in the unit repeated the given times.
    private const string Script = """
        const { findMatches, foldText } = await import(process.argv[1]);
        const [unit, times, query] = JSON.parse(process.argv[2]);
        console.log(JSON.stringify(findMatches(foldText(unit.repeat(times)), query)));
        """;

    private ServedLibrary library = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.library = new ServedLibrary("/js/text-search.js");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.library.Dispose();
    }

    [Test]
    public async Task AMatchIgnoresCaseDiacriticsAndTheKindOfSpace()
    {
        Assert.That(await this.Find("Lhůta BĚŽÍ dnes.", 1, "lhuta bezi dnes"), Is.EqualTo(Matches([0, 15])));
    }

    [Test]
    public async Task AMatchRunsAcrossALineEnd()
    {
        Assert.That(await this.Find("lhůta\nběží", 1, "LHUTA BEZI"), Is.EqualTo(Matches([0, 10])));
    }

    [Test]
    public async Task TheMatchesCoverAllOfTheText()
    {
        var matches = await this.Find("Řádek\n", 20000, "radek");
        int[] last = [119994, 119999];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(matches, Has.Length.EqualTo(20000), "a search counts the matches of the whole text, not of the lines on screen");
            Assert.That(matches[^1], Is.EqualTo(last));
        }
    }

    [Test]
    public async Task AMatchTakesTheMarksAfterItsLastLetter()
    {
        Assert.That(await this.Find("Lhu\u030Ata", 1, "lhu"), Is.EqualTo(Matches([0, 4])));
    }

    [Test]
    public async Task MatchesNeverOverlap()
    {
        Assert.That(await this.Find("aaaaa", 1, "aa"), Is.EqualTo(Matches([0, 2], [2, 4])));
    }

    [Test]
    public async Task AnOffsetCountsUtf16Units()
    {
        Assert.That(await this.Find("😀ž", 1, "z"), Is.EqualTo(Matches([2, 3])), "the editor counts positions in UTF-16 units");
    }

    [Test]
    public async Task AnEmptyQueryMatchesNothing()
    {
        Assert.That(await this.Find("abc", 1, ""), Is.Empty);
    }

    private static int[][] Matches(params int[][] matches)
    {
        return matches;
    }

    private async Task<int[][]> Find(string unit, int times, string query)
    {
        var output = await this.library.Run(Script, JsonSerializer.Serialize<object[]>([unit, times, query]));

        return JsonSerializer.Deserialize<int[][]>(output)!;
    }
}
