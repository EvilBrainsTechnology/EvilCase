using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

public static class Bootstrap
{
    private static ClientLogSink? sink;

    /// <summary>
    /// Logs to the browser console and buffers events for the server. The application supplies an
    /// <see cref="IClientLogUploader"/> and calls <see cref="StartClientLogging"/> once the host is built.
    /// The upload path is the endpoint the batches go to; a successful request to it is not logged,
    /// because the next upload would ship that log and log again.
    /// </summary>
    public static WebAssemblyHostBuilder AddClientLogging(
        this WebAssemblyHostBuilder builder,
        string settingsPath,
        string machineIdStorageKey,
        string uploadPath)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Configuration.GetSection(settingsPath).Get<ClientLoggingOptions>() ?? new ClientLoggingOptions();

        sink = new ClientLogSink();

        SelfLog.Enable(message => Console.Error.WriteLine(message));

        // The pipeline lets through whatever either destination asks for; each one then restricts
        // itself. Sharing a single threshold would silently cap the more verbose of the two.
#pragma warning disable RCS0054 // Fix formatting of a call chain
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(MoreVerbose(options.MinimumLevel, options.ServerMinimumLevel))
            .Enrich.FromLogContext()
            .WriteTo.BrowserConsole(restrictedToMinimumLevel: options.MinimumLevel, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Logger(ShipToServer(options))
            .CreateLogger();
#pragma warning restore RCS0054

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        builder.Services.AddRequestContext(machineIdStorageKey);
        builder.Services.AddSingleton(
            provider => new ClientHttpLogger(provider.GetRequiredService<ILogger<ClientHttpLogger>>(), uploadPath));

        // The route stays relative: the app may be served from a sub-path, which the base address carries.
        builder.Services.AddSingleton<IPageUnloadFlusher>(
            provider => new PageUnloadFlusher(
                provider.GetRequiredService<IJSRuntime>(),
                sink,
                provider.GetRequiredService<NavigationManager>().ToAbsoluteUri(uploadPath.TrimStart('/')).ToString()));

        return builder;
    }

    public static IServiceCollection AddRequestContext(this IServiceCollection services, string machineIdStorageKey)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IClientIdentity>(
            provider => new ClientIdentity(provider.GetRequiredService<IJSRuntime>(), machineIdStorageKey));

        return services.AddTransient<RequestContextHandler>();
    }

    public static WebAssemblyHost StartClientLogging(this WebAssemblyHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (sink is null)
            throw new InvalidOperationException("Client logging was not configured. Call AddClientLogging on WebAssemblyHostBuilder at startup.");

        sink.Start(host.Services.GetRequiredService<IClientLogUploader>(), host.Services.GetRequiredService<NavigationManager>());

        _ = host.Services.GetRequiredService<IPageUnloadFlusher>().StartAsync();

        return host;
    }

    // Verbose is the lowest LogEventLevel and Fatal the highest.
    private static LogEventLevel MoreVerbose(LogEventLevel first, LogEventLevel second) => first < second ? first : second;

    private static Action<LoggerConfiguration> ShipToServer(ClientLoggingOptions options) =>
        shipped => shipped.WriteTo.Sink(sink!, options.ServerMinimumLevel);
}
