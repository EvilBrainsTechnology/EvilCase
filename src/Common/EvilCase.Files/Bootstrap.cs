using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Files;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseFiles(this IServiceCollection services, string fileSettingsPath)
    {
        services
            .AddOptions<FileSettings>()
            .BindConfiguration(fileSettingsPath, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Singleton: the store holds a root path and nothing else.
        services.AddSingleton<IFileBlobStore, FileBlobStore>();

        return services;
    }
}
