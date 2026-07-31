using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Api.Client;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseApiClient(
        this IServiceCollection services,
        Uri baseAddress,
        Action<IHttpClientBuilder>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        // Generated routes are relative, so the app survives being served from a sub-path. Without the
        // trailing slash the last segment of the base address would be replaced instead of kept.
        var root = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");

        return services.AddGeneratedApiClients(client => client.BaseAddress = root, configureClient);
    }
}
