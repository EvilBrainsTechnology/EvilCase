using System.Threading.RateLimiting;
using DotNetEnv;
using EvilBrains.Collections;
using EvilBrains.EvilCase.Api;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Host;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Local development secrets are put into the process environment, so from here on every environment
// reads them through the same AddEnvironmentVariables provider. This has to run before CreateBuilder,
// which is where that provider snapshots the environment. NoClobber leaves a real environment
// variable in place, TraversePath walks up from bin/ to the project directory (dotnet run keeps the
// caller's working directory, so the file cannot be found relative to it).
if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), Environments.Development, StringComparison.OrdinalIgnoreCase))
    Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"), LoadOptions.NoClobber().TraversePath());

var builder = WebApplication
    .CreateBuilder(args);

#pragma warning disable RCS0054 // Fix formatting of a call chain
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty(AppSource.PropertyName, AppSource.Server)
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);
#pragma warning restore RCS0054

// Seq is wired here rather than through the Serilog section, which has no way to leave a sink out. The
// server URL is the only switch: every environment names its own server and one that names none logs to
// the console only.
var seq = builder.Configuration.GetSection("EvilBrains:EvilCase:Logging:Seq");
var seqServerUrl = seq["ServerUrl"].NullIfEmpty();

if (seqServerUrl is not null)
    loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seq["ApiKey"].NullIfEmpty(), formatProvider: CultureInfo.InvariantCulture);

Log.Logger = loggerConfiguration.CreateLogger();

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // The proxy is another container, so its address is neither loopback nor known in advance.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        // With no known proxy to check against, this is the whole defence: only the entry the proxy in
        // front appended is read, so a caller cannot dictate its own address and scheme by sending a
        // longer chain. It also means the container must not be reachable past that proxy.
        options.ForwardLimit = 1;
    });

const string authPathPrefix = "/api/auth";

const string apiReferencePath = "/scalar";

// Well above a person signing in and retrying, far below what it takes to keep the container busy.
var authWindow = new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

// The browser sink uploads at most once per second, so this is a couple of tabs on the same address.
var clientLogWindow = new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) };

// The two anonymous entry points a single caller can make expensive: login pays 600k PBKDF2 iterations
// per request, an unknown e-mail included, and the log upload accepts 4 MB batches whose successful
// requests are deliberately not logged. Nothing else is limited, health probes among them.
builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                if (context.Request.Path.StartsWithSegments(authPathPrefix, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("auth|" + ClientAddress(context), _ => authWindow);

                if (context.Request.Path.StartsWithSegments(ClientLogRoute.Path, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("client-logs|" + ClientAddress(context), _ => clientLogWindow);

                return RateLimitPartition.GetNoLimiter("unlimited");
            });

        options.OnRejected = (context, _) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            return ValueTask.CompletedTask;
        };
    });

builder.AddEvilCaseAuth("EvilBrains:EvilCase:Auth");

// The logger is passed explicitly: the parameterless overload does not register Serilog.ILogger.
builder.Host.UseSerilog(Log.Logger);

builder.Services.ConfigureServices();

var app = builder.Build();

// Before anything is served: the build and the schema it queries have to match. Turn it off where the
// schema is rolled out separately, or where more than one instance starts at once.
if (app.Configuration.GetValue("EvilBrains:EvilCase:Database:MigrateOnStartup", defaultValue: true))
{
    try
    {
        await app.MigrateEvilCaseDatabaseAsync();
    }
    catch (Exception exception)
    {
        // The application is about to die and the sinks that batch would never ship this, so it is
        // logged and flushed here. Rethrown: a schema the build does not expect must not be served.
        Log.Fatal(exception, "Database migration failed, the application will not start");
        await Log.CloseAndFlushAsync();

        throw;
    }
}

// Behind a TLS terminating proxy every request arrives over plain HTTP and from the proxy's address.
// The forwarded headers restore the caller's scheme and address for the pipeline below.
if (app.Configuration.GetValue("EvilBrains:EvilCase:Hosting:BehindReverseProxy", defaultValue: false))
    app.UseForwardedHeaders();

// Without it an unhandled exception answers with a bodyless 500 instead of the problem details shape
// every other API error uses. It sits above the request logging, which logs the exception and rethrows.
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// The Scalar UI loads its bundle from a CDN and configures it with an inline script, so the policy would
// break it; it is mapped in Development only.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments(apiReferencePath, StringComparison.OrdinalIgnoreCase),
    branch => branch.UseMiddleware<SecurityHeadersMiddleware>());

// Only the API is logged: the frontend, its assets and the health probes would otherwise bury it.
// The log upload is the exception inside it — logging it would be shipped by the next upload.
app.UseRequestLogging(loggedPaths: ["/api"], quietPaths: [ClientLogRoute.Path]);

// Turned off where something in front already redirects, or where nothing terminates TLS at all.
if (app.Configuration.GetValue("EvilBrains:EvilCase:Hosting:HttpsRedirection", defaultValue: true))
{
    // Probes usually arrive over plain HTTP; a redirect carries no body and counts as a failed probe.
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments(HealthCheckPaths.Prefix, StringComparison.OrdinalIgnoreCase),
        branch => branch.UseHttpsRedirection());
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Behind the forwarded headers, so a partition is the caller's address rather than the proxy's.
app.UseRateLimiter();

// Explicit, although the builder would insert it: the implicit one runs ahead of the whole pipeline
// below, which would log an authentication failure without the request identifiers and against the
// proxy's address.
app.UseAuthentication();
app.UseAuthorization();

app.MapEvilCaseApi();

if (app.Environment.IsDevelopment())
    app.MapEvilCaseApiReference();

app.MapFallbackToFile("index.html");

await app.RunAsync();

// UseSerilog was handed a logger it does not own, so nothing else flushes it on a normal shutdown.
await Log.CloseAndFlushAsync();

static string ClientAddress(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
