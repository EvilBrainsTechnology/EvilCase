using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logs;
using Microsoft.AspNetCore.Components;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace EvilBrains.EvilCase.App.Logging;

/// <summary>
/// Buffers log events in the browser and ships them to the API in batches.
/// The sink is created before the host exists, so the API client arrives later through <see cref="Start"/>;
/// events emitted until then are buffered.
/// </summary>
internal sealed class ApiLogSink : ILogEventSink
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

    public void Start(ILogsClient client, NavigationManager navigationManager)
    {
        this.navigation = navigationManager;

        _ = this.ShipAsync(client);
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

    // ScalarValue.ToString() quotes strings, which would arrive at the API quoted twice.
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

    private async Task ShipAsync(ILogsClient client)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync())
            await this.FlushAsync(client);
    }

    private async Task FlushAsync(ILogsClient client)
    {
        while (!this.queue.IsEmpty)
        {
            var entries = new List<ClientLogEntry>(ClientLogBatch.MaxEntries);
            while (entries.Count < ClientLogBatch.MaxEntries && this.queue.TryDequeue(out var entry))
                entries.Add(entry);

            try
            {
                await client.WriteClientLogs(new ClientLogBatch { Entries = entries });
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
            {
                // The batch is dropped and the rest waits for the next tick. Logging the failure through Serilog
                // would feed the sink that just failed, so it goes to Serilog's own diagnostic channel.
                SelfLog.WriteLine("Shipping {0} log entries to the API failed: {1}", entries.Count, exception);

                return;
            }
        }
    }

    private ClientLogEntry ToEntry(LogEvent logEvent) => new()
    {
        Timestamp = logEvent.Timestamp,
        Level = ToClientLevel(logEvent.Level),

        // The template travels unrendered so the API can log the event with its properties intact.
        MessageTemplate = Truncate(logEvent.MessageTemplate.Text, ClientLogEntry.MessageTemplateMaxLength),
        Properties = ToProperties(logEvent),
        Category = Truncate(SourceContext(logEvent), ClientLogEntry.CategoryMaxLength),
        Exception = Truncate(logEvent.Exception?.ToString(), ClientLogEntry.ExceptionMaxLength),
        Url = Truncate(this.navigation?.Uri, ClientLogEntry.UrlMaxLength),
    };
}
