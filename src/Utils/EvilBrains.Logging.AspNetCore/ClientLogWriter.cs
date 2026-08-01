using System.Diagnostics.CodeAnalysis;
using EvilBrains.Logging.Contract;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Rebuilds a Serilog event from an untrusted browser log entry. The parsed message template is the
/// allow-list of property names: whatever the template does not reference is dropped, and so is
/// anything the server owns. The event timestamp is the server clock, because browser clocks are
/// arbitrary; the browser value is kept as ClientTimestamp.
/// </summary>
internal sealed class ClientLogWriter : IClientLogWriter
{
    private const int MaxAlignmentWidth = 64;

    private static readonly MessageTemplateParser Parser = new();

    private static readonly MessageTemplate FallbackTemplate = Parser.Parse("{ClientMessage:l}");

    private readonly Serilog.ILogger logger;

    public ClientLogWriter(Serilog.ILogger logger, string sourceContext)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContext);

        this.logger = logger.ForContext(Constants.SourceContextPropertyName, sourceContext);
    }

    public void Write(ClientLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var level = ToLogEventLevel(entry.Level);
        if (!this.logger.IsEnabled(level))
            return;

        var (template, properties) = Bind(Sanitize(entry.MessageTemplate, ClientLogEntry.MessageTemplateMaxLength), entry.Properties);

        // Set on the event, so they win over the server's own enrichers and log context.
        properties.Add(new(AppSource.PropertyName, new ScalarValue(AppSource.Client)));

        AddIdentifier(properties, RequestContextPropertyNames.RequestId, entry.RequestId);
        AddIdentifier(properties, RequestContextPropertyNames.CorrelationId, entry.CorrelationId);

        properties.Add(new("ClientTimestamp", new ScalarValue(entry.Timestamp)));

        if (entry.Category is not null)
            properties.Add(new("ClientCategory", new ScalarValue(Sanitize(entry.Category, ClientLogEntry.CategoryMaxLength))));

        if (entry.Url is not null)
            properties.Add(new("ClientUrl", new ScalarValue(Sanitize(entry.Url, ClientLogEntry.UrlMaxLength))));

        var exception = entry.Exception is null
            ? null
            : new ClientLogException(Sanitize(entry.Exception, ClientLogEntry.ExceptionMaxLength));

        this.logger.Write(new LogEvent(DateTimeOffset.Now, level, exception, template, properties));
    }

    private static (MessageTemplate Template, List<LogEventProperty> Properties) Bind(string text, IReadOnlyDictionary<string, string>? values)
    {
        MessageTemplate template;
        try
        {
            template = Parser.Parse(text);
        }
        catch (Exception exception) when (exception is OverflowException or ArgumentException or FormatException)
        {
            // The parser degrades malformed templates to plain text, except for alignments that overflow.
            return (FallbackTemplate, [new("ClientMessage", new ScalarValue(text))]);
        }

        var properties = new List<LogEventProperty>();
        if (values is null || values.Count == 0)
            return (template, properties);

        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in template.Tokens)
        {
            if (properties.Count == ClientLogEntry.MaxProperties)
                break;

            // A wide alignment renders as that many characters, so an unbound hole is the cheap way out.
            if (token is not PropertyToken property || property.Alignment is { Width: > MaxAlignmentWidth })
                continue;

            var name = property.PropertyName;
            if (ReservedLogPropertyNames.Contains(name) || !bound.Add(name) || !values.TryGetValue(name, out var value))
                continue;

            properties.Add(new(name, new ScalarValue(Sanitize(value, ClientLogEntry.PropertyValueMaxLength))));
        }

        return (template, properties);
    }

    /// <summary>
    /// The browser tells which request an entry belongs to; it is reformatted rather than trusted, and
    /// an unparseable value leaves the identifier of the upload in place.
    /// </summary>
    private static void AddIdentifier(List<LogEventProperty> properties, string name, string? value)
    {
        if (Guid.TryParse(value, out var id))
            properties.Add(new(name, new ScalarValue(id.ToString("D", CultureInfo.InvariantCulture))));
    }

    /// <summary>
    /// Control characters are stripped so a browser cannot forge lines in the plain text console sink.
    /// The value is nullable because the payload is JSON: a null inside the property dictionary passes
    /// model validation, which covers properties and never dictionary values.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    private static string? Sanitize(string? value, int maxLength)
    {
        if (value is null)
            return null;

        var text = Truncate(value, maxLength);

        return text.Any(char.IsControl) ? string.Concat(text.Where(x => !char.IsControl(x))) : text;
    }

    /// <summary>
    /// Cutting between a high and a low surrogate leaves a lone surrogate, which no UTF-16 consumer accepts.
    /// </summary>
    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength)];

    private static LogEventLevel ToLogEventLevel(ClientLogLevel level) => level switch
    {
        ClientLogLevel.Verbose => LogEventLevel.Verbose,
        ClientLogLevel.Debug => LogEventLevel.Debug,
        ClientLogLevel.Information => LogEventLevel.Information,
        ClientLogLevel.Warning => LogEventLevel.Warning,
        ClientLogLevel.Error => LogEventLevel.Error,
        ClientLogLevel.Fatal => LogEventLevel.Fatal,
        _ => LogEventLevel.Information,
    };
}
