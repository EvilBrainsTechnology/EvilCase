using Microsoft.Extensions.Configuration;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// A Seq server URL reaches the application from the environment only. A default in a shipped settings
/// file would make every clone ship its logs to that server unasked, and nobody would notice for months.
/// The browser has no Seq of its own: it uploads to this host, which owns the sink.
/// </summary>
public class SeqServerUrlTests
{
    private const string SeqServerUrlKey = "EvilBrains:EvilCase:Logging:Seq:ServerUrl";

    private static readonly string[] HostSettingsFiles = ["appsettings.Development.json", "appsettings.json"];

    /// <summary>
    /// The host's settings files travel to this assembly's output directory through the project
    /// reference, so what is asserted here is what a run actually reads.
    /// </summary>
    private static IEnumerable<string> ShippedSettingsFiles => Directory.EnumerateFiles(AppContext.BaseDirectory, "appsettings*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);

    /// <summary>
    /// Without this the copy could stop happening and every case below would silently pass on an empty
    /// source.
    /// </summary>
    [Test]
    public void TheHostSettingsFilesAreTheOnesUnderTest()
    {
        Assert.That(
            ShippedSettingsFiles,
            Is.SupersetOf(HostSettingsFiles),
            "the host's settings files must reach the test output, or nothing here is being asserted");
    }

    [TestCaseSource(nameof(ShippedSettingsFiles))]
    public void NoSettingsFileNamesASeqServer(string fileName)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName))
            .Build();

        Assert.That(
            configuration[SeqServerUrlKey],
            Is.Null.Or.Empty,
            $"{fileName} must name no Seq server, or a fresh clone ships its logs there unasked");
    }

    /// <summary>
    /// The Serilog section is the other way in: Program.cs switches only on the server URL above, so a
    /// sink configured there would ship regardless of it. `WriteTo` is one of several shapes that reach
    /// the sink — `AuditTo` and a sub-logger's nested arrays do too — so the whole file is scanned for
    /// what they share: a `Name` naming the sink.
    /// </summary>
    [TestCaseSource(nameof(ShippedSettingsFiles))]
    public void NoSettingsFileConfiguresASeqSink(string fileName)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName))
            .Build();

        // The key is matched case-insensitively, because a lowercase "name" binds just as well; the
        // value case-sensitively, because Serilog resolves the sink method by its exact name.
        var seqSinks = configuration
            .AsEnumerable()
            .Where(entry => entry.Key.EndsWith(":Name", StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.Value, "Seq", StringComparison.Ordinal))
            .Select(entry => entry.Key);

        Assert.That(
            seqSinks,
            Is.Empty,
            $"{fileName} must configure no Seq sink anywhere, which the server URL switch does not reach");
    }
}
