using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

public static class Bootstrap
{
    private static ClientLogSink? sink;

    /// <summary>
    /// The application registers an IClientLogUploader and calls StartClientLogging once the host is built.
    /// </summary>
    public static WebAssemblyHostBuilder AddClientLogging(
        this WebAssemblyHostBuilder builder,
        string settingsPath,
        string machineIdStorageKey,
        string uploadPath)
    {
        var options = builder.Configuration.GetSection(settingsPath).Get<ClientLoggingOptions>() ?? new ClientLoggingOptions();

        sink = new ClientLogSink();

        SelfLog.Enable(static message => Console.Error.WriteLine(message));

        Log.Logger = CreateLogger(builder.Services, options, sink);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        builder.Services.AddRequestContext(machineIdStorageKey);
        builder.Services.AddSingleton(
            provider => new ClientHttpLogger(provider.GetRequiredService<ILogger<ClientHttpLogger>>(), uploadPath));

        // The route stays relative: the app may be served from a sub-path, which the base address carries.
        builder.Services.AddSingleton<IPageUnloadFlusher>(
            provider =>
            {
                var navigation = provider.GetRequiredService<NavigationManager>();

                return new PageUnloadFlusher(provider.GetRequiredService<IJSRuntime>(), sink, navigation.ToAbsoluteUri(uploadPath.TrimStart('/')).ToString());
            });

        return builder;
    }

    public static IServiceCollection AddRequestContext(this IServiceCollection services, string machineIdStorageKey)
    {
        services.AddSingleton<IClientIdentity>(
            provider => new ClientIdentity(provider.GetRequiredService<IJSRuntime>(), machineIdStorageKey));

        return services.AddTransient<RequestContextHandler>();
    }

    public static WebAssemblyHost StartClientLogging(this WebAssemblyHost host)
    {
        if (sink is null)
            throw new InvalidOperationException("Client logging was not configured. Call AddClientLogging on WebAssemblyHostBuilder at startup.");

        sink.Start(host.Services.GetRequiredService<IClientLogUploader>(), host.Services.GetRequiredService<NavigationManager>());

        var flusher = host.Services.GetRequiredService<IPageUnloadFlusher>();
        _ = flusher.Start();

        return host;
    }

    internal static Logger CreateLogger(IServiceCollection services, ClientLoggingOptions options, ClientLogSink shipped)
    {
        // The host registers Blazor's runtime as an instance. A runtime of the sink's own numbers its
        // calls apart from Blazor's, and its answers complete Blazor's calls of the same number.
        var jsRuntime = (IJSRuntime)services.Last(static service => service.ServiceType == typeof(IJSRuntime)).ImplementationInstance!;

        // The pipeline lets through whatever either destination asks for; each one then restricts
        // itself. Sharing a single threshold would silently cap the more verbose of the two.
#pragma warning disable RCS0054 // Fix formatting of a call chain
        return new LoggerConfiguration()
            .MinimumLevel.Is(MoreVerbose(options.MinimumLevel, options.ServerMinimumLevel))
            .Enrich.FromLogContext()
            .WriteTo.BrowserConsole(restrictedToMinimumLevel: options.MinimumLevel, formatProvider: CultureInfo.InvariantCulture, jsRuntime: jsRuntime)
            .WriteTo.Logger(ShipToServer(options, shipped))
            .CreateLogger();
#pragma warning restore RCS0054
    }

    private static LogEventLevel MoreVerbose(LogEventLevel first, LogEventLevel second)
    {
        return first < second ? first : second;
    }

    private static Action<LoggerConfiguration> ShipToServer(ClientLoggingOptions options, ClientLogSink shipped)
    {
        return configuration => configuration.WriteTo.Sink(shipped, options.ServerMinimumLevel);
    }
}
