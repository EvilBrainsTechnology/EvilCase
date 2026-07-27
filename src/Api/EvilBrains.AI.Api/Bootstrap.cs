using EvilBrains.AI.Auth;
using EvilBrains.AI.Data;

namespace EvilBrains.AI.Api;

public static class Bootstrap
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "EvilBrains AI API";
                return Task.CompletedTask;
            });
        });

        services.AddEvilBrainsAIData();
        services.AddEvilBrainsAIAuth();

        return services;
    }
}
