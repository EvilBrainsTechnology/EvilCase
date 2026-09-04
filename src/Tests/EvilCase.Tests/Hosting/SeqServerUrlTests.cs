using Microsoft.Extensions.Configuration;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class SeqServerUrlTests
{
    private const string SeqServerUrlKey = "EvilBrains:EvilCase:Logging:Seq:ServerUrl";

    private static readonly string[] HostSettingsFiles = ["appsettings.Development.json", "appsettings.json"];

    private static IEnumerable<string> ShippedSettingsFiles => Directory.EnumerateFiles(AppContext.BaseDirectory, "appsettings*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);

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
    /// <c>WriteTo</c>, <c>AuditTo</c> and a sub-logger's nested arrays all reach a sink, so the scan is
    /// for the <c>Name</c> they share.
    /// </summary>
    [TestCaseSource(nameof(ShippedSettingsFiles))]
    public void NoSettingsFileConfiguresASeqSink(string fileName)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName))
            .Build();

        // The key is matched case-insensitively, because a lowercase "name" binds just as well; the
        // value case-sensitively, because Serilog resolves the sink method by its exact name.
        var seqSinks = configuration.AsEnumerable()
            .Where(static entry => entry.Key.EndsWith(":Name", StringComparison.OrdinalIgnoreCase))
            .Where(static entry => string.Equals(entry.Value, "Seq", StringComparison.Ordinal))
            .Select(static entry => entry.Key);

        Assert.That(
            seqSinks,
            Is.Empty,
            $"{fileName} must configure no Seq sink anywhere, which the server URL switch does not reach");
    }
}
