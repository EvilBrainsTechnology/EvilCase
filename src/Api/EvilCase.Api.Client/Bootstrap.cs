using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace EvilBrains.EvilCase.Api.Client;

public static class Bootstrap
{
    // Registration must live in this assembly: calling into it runs the module initializer
    // that registers the source-generated Refit clients (required on Blazor WebAssembly).
    public static IServiceCollection AddEvilCaseApiClient(this IServiceCollection services, Uri baseAddress)
    {
        services
            .AddRefitGeneratedClient<IEchoApi>()
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);

        return services;
    }
}
