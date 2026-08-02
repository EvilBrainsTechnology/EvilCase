using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Files;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseFiles(this IServiceCollection services, string fileSettingsPath)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<FileStoreSettings>()
            .BindConfiguration(fileSettingsPath, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFileStore, LocalFileStore>();

        return services;
    }
}
