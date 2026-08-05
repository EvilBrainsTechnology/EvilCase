# EvilBrains.Logging.AspNetCore

Server half of the logging pipeline. The wire contract lives in `EvilBrains.Logging.Contract`, the browser half in `EvilBrains.Logging.WebAssembly`.

## Setup

Serilog is configured in the host's `Program.cs` and handed to `UseSerilog(Log.Logger)`. The explicit overload is required: the parameterless one registers no `Serilog.ILogger`, which `AddClientLogWriter` resolves. The host owns the logger, so it also flushes it after `RunAsync`.

Server events are enriched with `AppSource = Server`.

In EvilCase, Seq is configured from `EvilBrains:EvilCase:Logging:Seq` (`ServerUrl`, `ApiKey`), never from the `Serilog` section, which holds only the console sink; an empty server URL logs to the console only, and Seq credentials stay on the server. No `appsettings.*.json` names a server: a URL reaches the application from the environment alone. The `Environment` property is enriched from `builder.Environment.EnvironmentName`, never from an `appsettings.*.json` of its own.

- `services.AddClientLogWriter(clientSourceContext)` — registers `IClientLogWriter`. The source context names the deployment the browser logs are recorded under, not the library.
- `app.UseRequestLogging(loggedPaths, quietPaths)` — request context logging followed by Serilog's request logging, in that order, so the completion event carries the identifiers.

## Request logging levels

`RequestLogLevelPolicy` narrows Serilog's defaults:

| Case | Level |
| --- | --- |
| Unhandled exception, or status ≥ 500 | `Error` |
| `OPTIONS` | `Verbose` |
| Path outside `loggedPaths` | `Verbose` |
| Path under `quietPaths`, status < 400 | `Verbose` |
| Everything else under `loggedPaths` | `Information` |

`Verbose` falls below the configured minimum, so those requests leave no completion log at all. An allow-list rather than a deny-list: the host also serves the frontend, `/_framework/*`, `/_content/*` and vendored CSS.

The quiet path is the client log upload endpoint. Logging a successful upload would ship that log in the next upload, which would log again. A rejected upload (`4xx`) is logged, because a failed batch is dropped rather than retried, so it settles. The route is `ClientLogRoute` in `EvilCase.Api.Contract` — the controller, the host's quiet path and the browser sink all take it from there; naming it again anywhere silently breaks both feedback-loop guards.

## Request context

`RequestContextLoggingMiddleware` reads `X-Request-Id`, `X-Correlation-Id`, `X-Session-Id` and `X-Machine-Id`. Headers are untrusted: a single well-formed GUID is accepted and re-formatted, anything else yields no property at all (no `unknown` placeholder). They are pushed into the Serilog `LogContext` as `XRequestId`, `XCorrelationId`, `XSessionId` and `XMachineId`.

The `X` prefix is load-bearing. `RequestId` belongs to ASP.NET Core: it opens a scope per request holding the `TraceIdentifier`, and a scope property reaches an event ahead of the log context. A shared name would leave everything logged through `ILogger<T>` carrying the connection-local identifier while Serilog's own completion event carried the caller's. The middleware pushes the trace identifier under `RequestId` as well, because that scope does not reach the completion event.

## ClientLogWriter

Rebuilds a Serilog `LogEvent` from a `ClientLogEntry`. The endpoint is anonymous, so the whole payload is hostile input:

- The parsed message template is the allow-list of property names. A property the template does not reference is dropped, and so is any name in `ReservedLogPropertyNames` — properties carried on an event beat enrichers, so a browser entry must not be able to shadow one.
- At most `ClientLogEntry.MaxProperties` (16) properties are bound; a name binds once.
- `MessageTemplateParser.Parse` degrades malformed templates to plain text, but throws on alignments that overflow; that failure falls back to logging the raw text under `{ClientMessage:l}`. An alignment wider than 64 stays unbound — padding is only rendered for bound properties.
- Every string is sanitized — template, property values, category, URL and exception text — truncated to its contract maximum and stripped of control characters, so the plain text console sink cannot be forged. `Sanitize` accepts null: the payload is JSON, and a null inside the property dictionary passes model validation, which covers properties and never dictionary values.
- The event timestamp is the server clock; the browser value is kept as `ClientTimestamp`. Browser clocks are arbitrary and would corrupt the Seq timeline.
- `AppSource = Client` is set on the event.
- `RequestId` and `CorrelationId` from the entry are accepted only as GUIDs and re-formatted. An entry written while an API call was in flight therefore shares an identifier with the server side of that call. An entry written outside a call has none and inherits the identifiers of the upload that carried it — correlate those through `SessionId`, `MachineId`, `ClientUrl` and `ClientTimestamp`.
- Browser exception text arrives as a `ClientLogException`.
