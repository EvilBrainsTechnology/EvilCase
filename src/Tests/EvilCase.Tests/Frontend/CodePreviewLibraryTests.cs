using System.Text.Json;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CodePreviewLibraryTests
{
    // Prints every call of the library as one JSON array: { value } or { error }.
    private const string Calls = """
        const { formatCode } = await import(process.argv[1]);
        const calls = JSON.parse(process.argv[2]);
        const results = [];
        for (const [text, language] of calls) {
            try {
                results.push({ value: await formatCode(text, language) });
            } catch (error) {
                results.push({ error: error.name });
            }
        }
        console.log(JSON.stringify(results));
        """;

    private ServedLibrary library = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.library = new ServedLibrary("/lib/code-preview/code-preview.min.js");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.library.Dispose();
    }

    [TestCase("{a:1}")]
    [TestCase("{'a':1}")]
    [TestCase("{\"a\":[1,]}")]
    [TestCase("{\"a\":NaN}")]
    public async Task TextThatIsNotJsonDoesNotFormatAsJson(string text)
    {
        var result = (await this.Run((text, "json"))).Single();

        Assert.That(result.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null), "a lenient parser would print it as valid JSON");
    }

    [Test]
    public async Task JsonFormatsWithItsNumbersAsWritten()
    {
        var result = (await this.Run(("{\"a\":1.10,\"b\":1E5,\"c\":12345678901234567890}", "json"))).Single();

        Assert.That(result.GetProperty("value").GetString(), Is.EqualTo("{\n  \"a\": 1.10,\n  \"b\": 1E5,\n  \"c\": 12345678901234567890\n}\n"));
    }

    [Test]
    public async Task AFailureOtherThanInvalidTextThrows()
    {
        var deep = new string('[', 20000) + new string(']', 20000);
        var result = (await this.Run((deep, "json"))).Single();

        Assert.That(result.TryGetProperty("error", out _), Is.True, "valid JSON too deep to format is not invalid JSON");
    }

    [Test]
    public async Task TheFormattingLibraryBundlesNoHighlighter()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var licenses = await client.GetStringAsync(new Uri("/lib/code-preview/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(licenses, Does.Contain("===== prettier "));
            Assert.That(licenses, Does.Not.Contain("===== highlight.js "), "the preview highlights with the editor's library");
        }
    }

    private async Task<JsonElement[]> Run(params (string Text, string Language)[] calls)
    {
        var output = await this.library.Run(Calls, JsonSerializer.Serialize(calls.Select(static call => new[] { call.Text, call.Language })));

        return JsonSerializer.Deserialize<JsonElement[]>(output)!;
    }
}
