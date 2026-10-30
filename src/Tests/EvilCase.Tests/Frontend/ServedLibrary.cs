using System.Diagnostics;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Libraries the app serves, run in Node.js, a build prerequisite of the app.
/// </summary>
internal sealed class ServedLibrary : IDisposable
{
    private readonly string[] paths;

    private readonly EvilCaseHost host = new();

    private readonly HttpClient client;

    private readonly string directory = Directory.CreateTempSubdirectory("evilcase-library-").FullName;

    public ServedLibrary(params string[] paths)
    {
        this.paths = paths;
        this.client = this.host.CreateClient();
    }

    /// <summary>
    /// Runs the module script with the libraries' file URLs as <c>process.argv[1]</c> onwards and the arguments after
    /// them, and returns what it printed.
    /// </summary>
    public async Task<string> Run(string script, params string[] arguments)
    {
        var libraries = this.paths
            .Select((_, index) => Path.Combine(this.directory, string.Create(CultureInfo.InvariantCulture, $"library{index}.mjs")))
            .ToList();

        for (var index = 0; index < libraries.Count; index++)
        {
            if (!File.Exists(libraries[index]))
                await File.WriteAllBytesAsync(libraries[index], await this.client.GetByteArrayAsync(new Uri(this.paths[index], UriKind.Relative)));
        }

        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

        start.ArgumentList.Add("--input-type=module");
        start.ArgumentList.Add("--eval");
        start.ArgumentList.Add(script);

        foreach (var library in libraries)
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
