using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace EvilBrains.Secrets.Env;

public static class Bootstrap
{
    private const string EnvFileName = ".env";

    /// <summary>
    /// Adds secrets from the nearest <c>.env</c> file, searched from the application directory upwards
    /// (the working directory differs between <c>dotnet run</c> and the IDE). Does nothing when there
    /// is no such file.
    /// </summary>
    public static IConfigurationBuilder AddEnvFile(this IConfigurationBuilder builder)
    {
        var directory = FindEnvFileDirectory();
        if (directory is null)
            return builder;

        return builder.Add<EnvFileConfigurationSource>(source =>
        {
            // The default exclusion filters hide dot-prefixed files, so .env would never be seen.
            source.FileProvider = new PhysicalFileProvider(directory, ExclusionFilters.None);
            source.Path = EnvFileName;
        });
    }

    private static string? FindEnvFileDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, EnvFileName)))
                return directory.FullName;
        }

        return null;
    }
}
