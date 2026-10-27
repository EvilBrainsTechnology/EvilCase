using System.Diagnostics;
using System.Text;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Runs the file type library the app serves in Node.js, a build prerequisite of the app.
/// </summary>
public class FileTypeLibraryTests
{
    private const string Library = "/lib/file-type/file-type.min.js";

    // Prints the extension the library finds in the base64 content, or an empty line.
    private const string Detect = """
        const { fileTypeFromBuffer } = await import(process.argv[1]);
        const type = await fileTypeFromBuffer(new Uint8Array(Buffer.from(process.argv[2], 'base64')));
        console.log(type?.ext ?? '');
        """;

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    private string directory = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost();
        this.client = this.host.CreateClient();
        this.directory = Directory.CreateTempSubdirectory("evilcase-file-type-").FullName;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
        Directory.Delete(this.directory, recursive: true);
    }

    [TestCase("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Potvrzeni/>", "xml")]
    [TestCase("﻿<?xml version=\"1.0\"?><Potvrzeni/>", "xml")]
    [TestCase("%PDF-1.7\n%âãÏÓ\n", "pdf")]
    [TestCase("Prostý text bez typu.", "")]
    public async Task TheContentShowsItsExtension(string content, string extension)
    {
        Assert.That(await this.Run(Encoding.UTF8.GetBytes(content)), Is.EqualTo(extension));
    }

    private async Task<string> Run(byte[] content)
    {
        var library = Path.Combine(this.directory, "file-type.mjs");

        if (!File.Exists(library))
            await File.WriteAllBytesAsync(library, await this.client.GetByteArrayAsync(new Uri(Library, UriKind.Relative)));

        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

        start.ArgumentList.Add("--input-type=module");
        start.ArgumentList.Add("--eval");
        start.ArgumentList.Add(Detect);
        start.ArgumentList.Add(new Uri(library).AbsoluteUri);
        start.ArgumentList.Add(Convert.ToBase64String(content));

        using var node = Process.Start(start)!;

        var output = node.StandardOutput.ReadToEndAsync();
        var error = node.StandardError.ReadToEndAsync();

        await node.WaitForExitAsync();

        Assert.That(node.ExitCode, Is.Zero, await error);

        return (await output).Trim();
    }
}
