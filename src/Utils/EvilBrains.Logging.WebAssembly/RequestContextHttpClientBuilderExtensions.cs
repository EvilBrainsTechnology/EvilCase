using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.Logging.WebAssembly;

public static class RequestContextHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds the request, correlation, session and machine identifiers to every request of this client.
    /// </summary>
    public static IHttpClientBuilder AddRequestContextHeaders(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddHttpMessageHandler<RequestContextHandler>();
    }
}
