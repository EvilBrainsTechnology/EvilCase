using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// The DbContext is never built here, so no connection string is needed.
/// </summary>
public class FileStorageSettingsTests
{
    [Test]
    public void TheRootPathComesFromConfiguration()
    {
        using var provider = BuildProvider(rootPath: "/var/lib/evilcase/files");

        var settings = provider.GetRequiredService<FileStorageSettings>();

        Assert.That(settings.RootPath, Is.EqualTo("/var/lib/evilcase/files"));
    }

    [Test]
    public void TheRootPathIsRequired()
    {
        using var provider = BuildProvider(rootPath: null);

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<FileStorageSettings>());
    }

    private static ServiceProvider BuildProvider(string? rootPath)
    {
        var configurationValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (rootPath is not null)
            configurationValues["EvilBrains:EvilCase:Files:RootPath"] = rootPath;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurationValues).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEvilCaseData();

        return services.BuildServiceProvider();
    }
}
