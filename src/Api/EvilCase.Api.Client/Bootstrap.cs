using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Api.Client;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseApiClient(
        this IServiceCollection services,
        Uri baseAddress,
        Action<IHttpClientBuilder>? configureClient = null)
    {
        return services.AddGeneratedApiClients(client => client.BaseAddress = baseAddress, configureClient);
    }
}
