using System.Text;
using System.Text.Json;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTypeLibraryTests
{
    // Prints the extension the library finds in each base64 content as one JSON array; null where it finds none.
    private const string Detect = """
        const { fileTypeFromBuffer } = await import(process.argv[1]);
        const results = [];
        for (const content of JSON.parse(process.argv[2])) {
            const type = await fileTypeFromBuffer(new Uint8Array(Buffer.from(content, 'base64')));
            results.push(type?.ext ?? null);
        }
        console.log(JSON.stringify(results));
        """;

    private ServedLibrary library = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.library = new ServedLibrary("/lib/file-type/file-type.min.js");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.library.Dispose();
    }

    [Test]
    public async Task TheContentShowsItsExtension()
    {
        string[] contents =
        [
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Potvrzeni/>",
            "\uFEFF<?xml version=\"1.0\"?><Potvrzeni/>",
            "%PDF-1.7\n",
            "Prostý text bez typu.",
        ];
        var encoded = contents.Select(static content => Convert.ToBase64String(Encoding.UTF8.GetBytes(content)));

        var output = await this.library.Run(Detect, JsonSerializer.Serialize(encoded));

        Assert.That(JsonSerializer.Deserialize<string?[]>(output), Is.EqualTo(["xml", "xml", "pdf", null]));
    }
}
