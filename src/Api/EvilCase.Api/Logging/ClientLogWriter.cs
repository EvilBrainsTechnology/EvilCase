using EvilBrains.EvilCase.Api.Contract.Logs;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace EvilBrains.EvilCase.Api.Logging;

/// <summary>
/// Rebuilds a Serilog event from an untrusted browser log entry. The parsed message template is the
/// allow-list of property names: whatever the template does not reference is dropped, and so is
/// anything the server owns. The event timestamp is the server clock, because browser clocks are
/// arbitrary; the browser value is kept as ClientTimestamp.
/// </summary>
internal sealed class ClientLogWriter : IClientLogWriter
{
    private const string ClientSourceContext = "EvilBrains.EvilCase.App.Client";

    private const int MaxAlignmentWidth = 64;

    private static readonly MessageTemplateParser Parser = new();

    private static readonly MessageTemplate FallbackTemplate = Parser.Parse("{ClientMessage:l}");

    private readonly Serilog.ILogger logger;

    public ClientLogWriter(Serilog.ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger.ForContext(Constants.SourceContextPropertyName, ClientSourceContext);
    }

    public void Write(ClientLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var level = ToLogEventLevel(entry.Level);
        if (!this.logger.IsEnabled(level))
            return;

        var (template, properties) = Bind(Sanitize(entry.MessageTemplate, ClientLogEntry.MessageTemplateMaxLength), entry.Properties);

        properties.Add(new("ClientTimestamp", new ScalarValue(entry.Timestamp)));

        if (entry.Category is not null)
            properties.Add(new("ClientCategory", new ScalarValue(Sanitize(entry.Category, ClientLogEntry.CategoryMaxLength))));

        if (entry.Url is not null)
            properties.Add(new("ClientUrl", new ScalarValue(Sanitize(entry.Url, ClientLogEntry.UrlMaxLength))));

        var exception = entry.Exception is null ? null : new ClientLogException(entry.Exception);

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
    /// Control characters are stripped so a browser cannot forge lines in the plain text console sink.
    /// </summary>
    private static string Sanitize(string value, int maxLength)
    {
        var text = value.Length <= maxLength ? value : value[..maxLength];

        return text.Any(char.IsControl) ? string.Concat(text.Where(x => !char.IsControl(x))) : text;
    }

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
