using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.Logging.AspNetCore;

public static class Bootstrap
{
    /// <summary>
    /// Requires Serilog.ILogger in the container, which is what <c>UseSerilog(Log.Logger)</c> registers.
    /// </summary>
    public static IServiceCollection AddClientLogWriter(this IServiceCollection services, string clientSourceContext)
    {
        return services.AddSingleton<IClientLogWriter>(
            provider => new ClientLogWriter(provider.GetRequiredService<Serilog.ILogger>(), clientSourceContext));
    }
}
