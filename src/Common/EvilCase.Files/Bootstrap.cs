using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Files;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseFiles(this IServiceCollection services)
    {
        services
            .AddOptions<FileSettings>()
            .BindConfiguration("EvilBrains:EvilCase:Files", static options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Singleton: the store holds a root path and nothing else.
        services.AddSingleton<IFileBlobStore, FileBlobStore>();

        return services;
    }
}
