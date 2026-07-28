using EvilBrains.EvilCase.Api.Routing;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvilBrains.EvilCase.Api;

public static class Bootstrap
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, RefitRoutingApplicationModelProvider>());

        services.AddControllers();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "EvilCase API";
                return Task.CompletedTask;
            });
        });

        services.AddEvilCaseData();
        services.AddEvilCaseAuth();

        return services;
    }
}
