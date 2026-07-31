using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Components;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Buffers log events in the browser and ships them to the server in batches.
/// The sink is created before the host exists, so the uploader arrives later through <see cref="Start"/>;
/// events emitted until then are buffered.
/// </summary>
internal sealed class ClientLogSink : ILogEventSink
{
    private const int QueueCapacity = 500;

    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    private readonly ConcurrentQueue<ClientLogEntry> queue = [];

    private NavigationManager? navigation;

    public void Emit(LogEvent logEvent)
    {
        // Once the queue is full the app is either offline or flooding; drop instead of growing without bound.
        if (this.queue.Count >= QueueCapacity)
            return;

        this.queue.Enqueue(this.ToEntry(logEvent));
    }

    public void Start(IClientLogUploader uploader, NavigationManager navigationManager)
    {
        this.navigation = navigationManager;

        _ = this.ShipAsync(uploader);
    }

    [return: NotNullIfNotNull(nameof(value))]
    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];

    private static ClientLogLevel ToClientLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => ClientLogLevel.Verbose,
        LogEventLevel.Debug => ClientLogLevel.Debug,
        LogEventLevel.Information => ClientLogLevel.Information,
        LogEventLevel.Warning => ClientLogLevel.Warning,
        LogEventLevel.Error => ClientLogLevel.Error,
        LogEventLevel.Fatal => ClientLogLevel.Fatal,
        _ => ClientLogLevel.Information,
    };

    private static Dictionary<string, string>? ToProperties(LogEvent logEvent)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, value) in logEvent.Properties)
        {
            if (properties.Count == ClientLogEntry.MaxProperties)
                break;

            // Already shipped as Category.
            if (string.Equals(name, Constants.SourceContextPropertyName, StringComparison.Ordinal))
                continue;

            properties.Add(name, Truncate(RenderValue(value), ClientLogEntry.PropertyValueMaxLength));
        }

        return properties.Count == 0 ? null : properties;
    }

    // ScalarValue.ToString() quotes strings, which would arrive at the server quoted twice.
    private static string RenderValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => "null",
        ScalarValue { Value: string text } => text,
        ScalarValue { Value: var raw } => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString(format: null, CultureInfo.InvariantCulture),
    };

    private static string? SourceContext(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out var value) && value is ScalarValue { Value: string source }
            ? source
            : null;

    private async Task ShipAsync(IClientLogUploader uploader)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync())
            await this.FlushAsync(uploader);
    }

    private async Task FlushAsync(IClientLogUploader uploader)
    {
        while (!this.queue.IsEmpty)
        {
            var entries = new List<ClientLogEntry>(ClientLogBatch.MaxEntries);
            while (entries.Count < ClientLogBatch.MaxEntries && this.queue.TryDequeue(out var entry))
                entries.Add(entry);

            try
            {
                await uploader.UploadAsync(new ClientLogBatch { Entries = entries });
            }
            catch (ClientLogUploadException exception)
            {
                // The batch is dropped and the rest waits for the next tick. Logging the failure through Serilog
                // would feed the sink that just failed, so it goes to Serilog's own diagnostic channel.
                SelfLog.WriteLine("Shipping {0} log entries to the server failed: {1}", entries.Count, exception.InnerException ?? exception);

                return;
            }
        }
    }

    private ClientLogEntry ToEntry(LogEvent logEvent) => new()
    {
        Timestamp = logEvent.Timestamp,
        Level = ToClientLevel(logEvent.Level),

        // The template travels unrendered so the server can log the event with its properties intact.
        MessageTemplate = Truncate(logEvent.MessageTemplate.Text, ClientLogEntry.MessageTemplateMaxLength),
        Properties = ToProperties(logEvent),
        Category = Truncate(SourceContext(logEvent), ClientLogEntry.CategoryMaxLength),
        Exception = Truncate(logEvent.Exception?.ToString(), ClientLogEntry.ExceptionMaxLength),
        Url = Truncate(this.navigation?.Uri, ClientLogEntry.UrlMaxLength),
    };
}
