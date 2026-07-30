using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Logs;
using EvilBrains.EvilCase.Api.Logging;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("logs")]
public partial class LogsController(ILoggerFactory loggerFactory) : ControllerBase
{
    // Own source context so browser logs can be levelled and filtered separately from server logs.
    private const string ClientLoggerName = "EvilBrains.EvilCase.App.Client";

    [HttpPost("client")]
    public void WriteClientLogs([FromBody] ClientLogBatch batch)
    {
        var logger = loggerFactory.CreateLogger(ClientLoggerName);

        foreach (var entry in batch.Entries)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ClientTimestamp"] = entry.Timestamp,
                ["ClientCategory"] = entry.Category,
                ["ClientUrl"] = entry.Url,
            });

            var level = ToLogLevel(entry.Level);
            var exception = entry.Exception is null ? null : new ClientLogException(entry.Exception);

            LogClient(logger, level, exception, entry.Message);
        }
    }

    [LoggerMessage(Message = "{ClientMessage}")]
    private static partial void LogClient(ILogger logger, LogLevel level, Exception? exception, string clientMessage);

    private static LogLevel ToLogLevel(ClientLogLevel level) => level switch
    {
        ClientLogLevel.Verbose => LogLevel.Trace,
        ClientLogLevel.Debug => LogLevel.Debug,
        ClientLogLevel.Information => LogLevel.Information,
        ClientLogLevel.Warning => LogLevel.Warning,
        ClientLogLevel.Error => LogLevel.Error,
        ClientLogLevel.Fatal => LogLevel.Critical,
        _ => LogLevel.Information,
    };
}
