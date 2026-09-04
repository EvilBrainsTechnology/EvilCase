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

        services.AddSingleton<IFileBlobStore, FileBlobStore>();

        return services;
    }
}
