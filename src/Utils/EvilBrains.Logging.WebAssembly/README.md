# EvilBrains.Logging.WebAssembly

Browser half of the logging pipeline. The wire contract lives in `EvilBrains.Logging.Contract`, the server half in `EvilBrains.Logging.AspNetCore`.

## Setup

```csharp
builder.AddClientLogging(settingsPath, machineIdStorageKey, uploadPath);
// ...
var host = builder.Build();
host.StartClientLogging();
```

`StartClientLogging` is mandatory. The sink is created before the host exists, so it receives its `IClientLogUploader` and `NavigationManager` afterwards; without the call events buffer and are dropped at 500 with no error.

The application supplies `IClientLogUploader`. In `EvilCase.App` that is `ApiLogUploader`, over the generated `ILogsClient`; it translates transport failures into `ClientLogUploadException`, the only exception the sink swallows.

WebAssembly forces the differences from the server setup:

- There is no host builder, so `builder.Host.UseSerilog()` does not exist. `AddClientLogging` builds the logger and registers it with `ClearProviders()` + `AddSerilog` from `Serilog.Extensions.Logging`, not `Serilog.AspNetCore`.
- `Serilog.Settings.Configuration` is not used: it resolves sinks by assembly name through reflection, which breaks under WASM trimming. Levels are bound directly with `Get<ClientLoggingOptions>()`, because the logger exists before the container does.
- `EnableConfigurationBindingGenerator` on this project keeps that binding trim-safe — the property belongs to the project holding the call site.
- `ClientLoggingOptions` properties are `get; set;`. The generated binder assigns after construction and silently skips `init`-only properties, leaving the defaults in place with no error anywhere.

## Levels

Bound from the configured section (`ClientLogging` in `EvilCase.App/wwwroot/appsettings.json`):

- `MinimumLevel` (default `Information`) — the browser console.
- `ServerMinimumLevel` (default `Warning`) — the events shipped to the API.

The two are independent. The pipeline threshold is the more verbose of them and each destination restricts itself, so either one can be the looser.

Browser console output goes through `Serilog.Sinks.BrowserConsole`, which maps to real console levels instead of stdout.

## ClientLogSink

Buffers events and posts them in batches.

| Constant | Value |
| --- | --- |
| Queue capacity | 500 events, then dropped |
| Flush interval | 1 second |
| Batch size | `ClientLogBatch.MaxEntries` (100) |
| Properties per event | `ClientLogEntry.MaxProperties` (16) |
| Property value length | `ClientLogEntry.PropertyValueMaxLength` (512) |

Events keep their structure: the message template ships unrendered, so the server can rebuild the event with its properties intact. Values are rendered to strings, with scalars unquoted — `ScalarValue.ToString()` would arrive quoted twice. `SourceContext`, `XRequestId` and `XCorrelationId` are lifted out of the property bag into fields of their own; the current URL comes from `NavigationManager`.

A failed batch is dropped and the rest waits for the next tick. Failures go to Serilog's `SelfLog`, never through Serilog itself — that would feed the sink that just failed.

`PageUnloadFlusher` ships what is still buffered when the page goes away; the periodic loop drains once a second, so an event logged just before a reload would otherwise die with the runtime. The unload handler cannot await, so `Drain()` hands the serialised batch to JavaScript as a beacon body. `StartClientLogging` starts it and nothing awaits it: a browser that refuses the module costs the unload flush and nothing else.

## Request context and HTTP logging

On a generated API client:

- `AddRequestContextHeaders()` stamps every request with `X-Request-Id` (fresh per request), `X-Correlation-Id` (same value), `X-Session-Id` (one GUID per app load) and `X-Machine-Id`. The machine identifier lives in `localStorage` and survives reloads and browser restarts; `ClientIdentity` reads it through synchronous WebAssembly interop, which is what makes it available to the handler.
- `AddRequestLogging()` replaces the HTTP client factory's own logging (`RemoveAllLoggers()` + an `IHttpClientLogger`), whose four events per request use a template that cannot be changed and carry none of the request identifiers.

`ClientHttpLogger` writes one event per request, `HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms` at `Information`, or `HTTP {HttpMethod} {RequestPath} failed after {Elapsed} ms` at `Warning`. The identifiers ride in a logging scope, read back from the headers the handler stamped: they are for correlating, not for reading.

A successful request to the upload path is not logged at all — the next upload would ship that log and log again. A failed one is, and it settles because a failed batch is dropped rather than retried. The match on the upload path is a suffix match on a segment boundary, so it holds when the app is served from a sub-path.
