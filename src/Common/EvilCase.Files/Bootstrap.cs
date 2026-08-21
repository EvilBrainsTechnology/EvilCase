using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseFiles(this IServiceCollection services, string fileSettingsPath)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<FileSettings>()
            .BindConfiguration(fileSettingsPath, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<FileSettings>, FileSettingsValidator>();

        // Singleton: the store holds a root path and nothing else.
        services.AddSingleton<IFileBlobStore, FileBlobStore>();

        return services;
    }
}
