using EvilBrains.EvilCase.Api.Logging;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;

namespace EvilBrains.EvilCase.Api;

public static class Bootstrap
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "EvilCase API";
                return Task.CompletedTask;
            });
        });

        services.AddSingleton<ClientLogWriter>();

        services.AddEvilCaseData();
        services.AddEvilCaseAuth();

        return services;
    }
}
