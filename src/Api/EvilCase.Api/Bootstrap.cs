using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.Logging.AspNetCore;

namespace EvilBrains.EvilCase.Api;

public static class Bootstrap
{
    // The name browser logs are recorded under; it identifies the deployment, not the library.
    private const string ClientSourceContext = "EvilBrains.EvilCase.App.Client";

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

        services.AddClientLogWriter(ClientSourceContext);

        services.AddEvilCaseData();
        services.AddEvilCaseAuth();

        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
