using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.Logging.WebAssembly;

public static class RequestContextHttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddRequestContextHeaders(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<RequestContextHandler>();
    }

    /// <summary>
    /// Replaces the factory's own request logging, which writes four events per request under a
    /// template that cannot be changed and without the identifiers the request carries.
    /// </summary>
    public static IHttpClientBuilder AddRequestLogging(this IHttpClientBuilder builder)
    {
        return builder.RemoveAllLoggers().AddLogger<ClientHttpLogger>();
    }
}
