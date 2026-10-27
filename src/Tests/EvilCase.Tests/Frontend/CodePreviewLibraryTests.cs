using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Runs the code preview library the app serves in Node.js, a build prerequisite of the app.
/// </summary>
public partial class CodePreviewLibraryTests
{
    private const string Library = "/lib/code-preview/code-preview.min.js";

    // Prints every call of the library as one JSON array: { value } or { error }.
    private const string Calls = """
        const { formatCode, highlightCode } = await import(process.argv[1]);
        const calls = JSON.parse(process.argv[2]);
        const results = [];
        for (const [name, text, language] of calls) {
            try {
                results.push({ value: name === 'format' ? await formatCode(text, language) : highlightCode(text, language) });
            } catch (error) {
                results.push({ error: error.name });
            }
        }
        console.log(JSON.stringify(results));
        """;

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    private string directory = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost();
        this.client = this.host.CreateClient();
        this.directory = Directory.CreateTempSubdirectory("evilcase-code-preview-").FullName;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
        Directory.Delete(this.directory, recursive: true);
    }

    [TestCase("<script>alert(1)</script><img src=x onerror=alert(1)><b>x</b>", "html")]
    [TestCase("<?xml version=\"1.0\"?><a><![CDATA[<script>alert(1)</script>]]><img src=x onerror=alert(1)/></a>", "xml")]
    [TestCase("{\"a\": \"<script>alert(1)</script>\"}", "json")]
    [TestCase("a: <img src=x onerror=alert(1)>", "yaml")]
    public async Task HighlightingEscapesAllOfTheTextAndAddsOnlySpans(string text, string language)
    {
        var html = (await this.Run(("highlight", text, language))).Single().GetProperty("value").GetString()!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Spans.Replace(html, ""), Does.Not.Contain('<'), "the text of a file never becomes an element of the page");
            Assert.That(WebUtility.HtmlDecode(Spans.Replace(html, "")), Is.EqualTo(text));
        }
    }

    [Test]
    public async Task HtmlHighlightsItsEmbeddedScriptAndStyle()
    {
        var html = (await this.Run(("highlight", "<script>let a = 1;</script><style>b { color: red; }</style>", "html"))).Single().GetProperty("value").GetString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Contain("class=\"language-javascript\""));
            Assert.That(html, Does.Contain("class=\"language-css\""));
        }
    }

    [TestCase("{a:1}")]
    [TestCase("{'a':1}")]
    [TestCase("{\"a\":[1,]}")]
    [TestCase("{\"a\":NaN}")]
    public async Task TextThatIsNotJsonDoesNotFormatAsJson(string text)
    {
        var result = (await this.Run(("format", text, "json"))).Single();

        Assert.That(result.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null), "a lenient parser would print it as valid JSON");
    }

    [Test]
    public async Task JsonFormatsWithItsNumbersAsWritten()
    {
        var result = (await this.Run(("format", "{\"a\":1.10,\"b\":1E5,\"c\":12345678901234567890}", "json"))).Single();

        Assert.That(result.GetProperty("value").GetString(), Is.EqualTo("{\n  \"a\": 1.10,\n  \"b\": 1E5,\n  \"c\": 12345678901234567890\n}\n"));
    }

    [Test]
    public async Task AFailureOtherThanInvalidTextThrows()
    {
        var deep = new string('[', 20000) + new string(']', 20000);
        var result = (await this.Run(("format", deep, "json"))).Single();

        Assert.That(result.TryGetProperty("error", out _), Is.True, "valid JSON too deep to format is not invalid JSON");
    }

    [GeneratedRegex("</?span[^>]*>", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Spans { get; }

    private async Task<JsonElement[]> Run(params (string Name, string Text, string Language)[] calls)
    {
        var library = Path.Combine(this.directory, "code-preview.mjs");

        if (!File.Exists(library))
            await File.WriteAllBytesAsync(library, await this.client.GetByteArrayAsync(new Uri(Library, UriKind.Relative)));

        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

        start.ArgumentList.Add("--input-type=module");
        start.ArgumentList.Add("--eval");
        start.ArgumentList.Add(Calls);
        start.ArgumentList.Add(new Uri(library).AbsoluteUri);
        start.ArgumentList.Add(JsonSerializer.Serialize(calls.Select(static call => new[] { call.Name, call.Text, call.Language })));

        using var node = Process.Start(start)!;

        var output = node.StandardOutput.ReadToEndAsync();
        var error = node.StandardError.ReadToEndAsync();

        await node.WaitForExitAsync();

        Assert.That(node.ExitCode, Is.Zero, await error);

        return JsonSerializer.Deserialize<JsonElement[]>(await output)!;
    }
}
