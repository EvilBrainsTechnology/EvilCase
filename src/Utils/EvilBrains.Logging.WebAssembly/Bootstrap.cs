using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Filters;

namespace EvilBrains.Logging.WebAssembly;

public static class Bootstrap
{
    private const string HttpClientSource = "System.Net.Http.HttpClient";

    private static ClientLogSink? sink;

    /// <summary>
    /// Logs to the browser console and buffers events for the server. The application supplies an
    /// <see cref="IClientLogUploader"/> and calls <see cref="StartClientLogging"/> once the host is built.
    /// The upload client name is the name of the typed client interface that ships the batches; a
    /// successful upload of its own is dropped, otherwise every upload would log four more events and
    /// each of those would be shipped by the next one.
    /// </summary>
    public static WebAssemblyHostBuilder AddClientLogging(
        this WebAssemblyHostBuilder builder,
        string settingsPath,
        string machineIdStorageKey,
        string? uploadClientName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Configuration.GetSection(settingsPath).Get<ClientLoggingOptions>() ?? new ClientLoggingOptions();

        sink = new ClientLogSink();

        SelfLog.Enable(message => Console.Error.WriteLine(message));

        var configuration = new LoggerConfiguration();

        if (uploadClientName is not null)
            configuration.Filter.ByExcluding(SuccessfulUpload($"{HttpClientSource}.{uploadClientName}"));

#pragma warning disable RCS0054 // Fix formatting of a call chain
        Log.Logger = configuration
            .MinimumLevel.Is(options.MinimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.BrowserConsole(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Logger(ShipToServer(options))
            .CreateLogger();
#pragma warning restore RCS0054

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        builder.Services.AddRequestContext(machineIdStorageKey);

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

        return host;
    }

    /// <summary>
    /// Shipping a batch is itself an HTTP request. Dropping the successful ones is what keeps the sink
    /// from feeding itself; a failed upload is worth knowing about and settles on its own, because the
    /// batch that could not be sent is dropped rather than retried.
    /// </summary>
    private static Func<LogEvent, bool> SuccessfulUpload(string source)
    {
        var isUpload = Matching.FromSource(source);

        return logEvent => logEvent.Exception is null && isUpload(logEvent);
    }

    private static Action<LoggerConfiguration> ShipToServer(ClientLoggingOptions options) =>
        shipped => shipped.WriteTo.Sink(sink!, options.ServerMinimumLevel);
}
