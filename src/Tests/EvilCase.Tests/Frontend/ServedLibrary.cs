using System.Diagnostics;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A library the app serves, run in Node.js, a build prerequisite of the app.
/// </summary>
internal sealed class ServedLibrary : IDisposable
{
    private readonly string path;

    private readonly EvilCaseHost host = new();

    private readonly HttpClient client;

    private readonly string directory = Directory.CreateTempSubdirectory("evilcase-library-").FullName;

    public ServedLibrary(string path)
    {
        this.path = path;
        this.client = this.host.CreateClient();
    }

    /// <summary>
    /// Runs the module script with the library's file URL as <c>process.argv[1]</c> and the arguments after it, and
    /// returns what it printed.
    /// </summary>
    public async Task<string> Run(string script, params string[] arguments)
    {
        var library = Path.Combine(this.directory, "library.mjs");

        if (!File.Exists(library))
            await File.WriteAllBytesAsync(library, await this.client.GetByteArrayAsync(new Uri(this.path, UriKind.Relative)));

        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

        start.ArgumentList.Add("--input-type=module");
        start.ArgumentList.Add("--eval");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add(new Uri(library).AbsoluteUri);

        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var node = Process.Start(start)!;

        var output = node.StandardOutput.ReadToEndAsync();
        var error = node.StandardError.ReadToEndAsync();

        await node.WaitForExitAsync();

        Assert.That(node.ExitCode, Is.Zero, await error);

        return await output;
    }

    public void Dispose()
    {
        this.client.Dispose();
        this.host.Dispose();
        Directory.Delete(this.directory, recursive: true);
    }
}
