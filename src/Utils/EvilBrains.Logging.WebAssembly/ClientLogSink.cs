using System.Collections.Concurrent;
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

    private static readonly string[] Lifted =
    [
        Constants.SourceContextPropertyName,
        RequestContextPropertyNames.RequestId,
        RequestContextPropertyNames.CorrelationId,
    ];

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

        _ = this.Ship(uploader);
    }

    /// <summary>
    /// Takes the next batch out of the buffer. Public for the unload flush, which cannot await an upload
    /// and ships the batch itself.
    /// </summary>
    public ClientLogBatch? Drain()
    {
        if (this.queue.IsEmpty)
            return null;

        var entries = new List<ClientLogEntry>(ClientLogBatch.MaxEntries);
        while (entries.Count < ClientLogBatch.MaxEntries && this.queue.TryDequeue(out var entry))
            entries.Add(entry);

        return entries.Count == 0 ? null : new ClientLogBatch { Entries = entries };
    }

    private static ClientLogLevel ToClientLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => ClientLogLevel.Verbose,
            LogEventLevel.Debug => ClientLogLevel.Debug,
            LogEventLevel.Information => ClientLogLevel.Information,
            LogEventLevel.Warning => ClientLogLevel.Warning,
            LogEventLevel.Error => ClientLogLevel.Error,
            LogEventLevel.Fatal => ClientLogLevel.Fatal,
            _ => ClientLogLevel.Information,
        };
    }

    private static Dictionary<string, string>? ToProperties(LogEvent logEvent)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, value) in logEvent.Properties)
        {
            if (properties.Count == ClientLogEntry.MaxProperties)
                break;

            // Shipped as fields of their own.
            if (Lifted.Contains(name))
                continue;

            properties.Add(name, ClientLogText.Truncate(RenderValue(value), ClientLogEntry.PropertyValueMaxLength));
        }

        return properties.Count == 0 ? null : properties;
    }

    // ScalarValue.ToString() quotes strings, which would arrive at the server quoted twice.
    private static string RenderValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue { Value: null } => "null",
            ScalarValue { Value: string text } => text,
            ScalarValue { Value: var raw } => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString(format: null, CultureInfo.InvariantCulture),
        };
    }

    private static string? Text(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value) && value is ScalarValue { Value: string text }
            ? text
            : null;
    }

    /// <summary>
    /// Nothing observes this task, so an exception escaping the loop would stop the shipping for the
    /// rest of the application's life without a trace. Anything an uploader was not supposed to throw
    /// costs the current batch and nothing more.
    /// </summary>
    private async Task Ship(IClientLogUploader uploader)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await this.Flush(uploader);
            }
            catch (Exception exception)
            {
                SelfLog.WriteLine("Shipping log entries to the server failed unexpectedly: {0}", exception);
            }
        }
    }

    private async Task Flush(IClientLogUploader uploader)
    {
        while (this.Drain() is { } batch)
        {
            try
            {
                await uploader.Upload(batch);
            }
            catch (ClientLogUploadException exception)
            {
                // The batch is dropped and the rest waits for the next tick. Logging the failure through Serilog
                // would feed the sink that just failed, so it goes to Serilog's own diagnostic channel.
                SelfLog.WriteLine("Shipping {0} log entries to the server failed: {1}", batch.Entries.Count, exception.InnerException ?? exception);

                return;
            }
        }
    }

    private ClientLogEntry ToEntry(LogEvent logEvent)
    {
        return new()
        {
            Timestamp = logEvent.Timestamp,
            Level = ToClientLevel(logEvent.Level),

            // The template travels unrendered so the server can log the event with its properties intact.
            MessageTemplate = ClientLogText.Truncate(logEvent.MessageTemplate.Text, ClientLogEntry.MessageTemplateMaxLength),
            Properties = ToProperties(logEvent),
            RequestId = ClientLogText.Truncate(Text(logEvent, RequestContextPropertyNames.RequestId), ClientLogEntry.IdentifierMaxLength),
            CorrelationId = ClientLogText.Truncate(Text(logEvent, RequestContextPropertyNames.CorrelationId), ClientLogEntry.IdentifierMaxLength),
            Category = ClientLogText.Truncate(Text(logEvent, Constants.SourceContextPropertyName), ClientLogEntry.CategoryMaxLength),
            Exception = ClientLogText.Truncate(logEvent.Exception?.ToString(), ClientLogEntry.ExceptionMaxLength),
            Url = ClientLogText.Truncate(this.navigation?.Uri, ClientLogEntry.UrlMaxLength),
        };
    }
}
