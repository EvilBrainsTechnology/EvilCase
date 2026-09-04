using System.Threading.RateLimiting;
using DotNetEnv;
using EvilBrains.Collections;
using EvilBrains.EvilCase.Api;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Business;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Host;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Before CreateBuilder, which snapshots the environment. NoClobber keeps a real
// variable; TraversePath walks up from bin/ to the project.
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

// In code: the Serilog section cannot leave a sink out when no ServerUrl is set.
var seq = builder.Configuration.GetSection("EvilBrains:EvilCase:Logging:Seq");
var seqServerUrl = seq["ServerUrl"].NullIfEmpty();

if (seqServerUrl is not null)
    loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seq["ApiKey"].NullIfEmpty(), formatProvider: CultureInfo.InvariantCulture);

Log.Logger = loggerConfiguration.CreateLogger();

builder.Services.Configure<ForwardedHeadersOptions>(
    static options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // The proxy is another container, so its address is neither loopback nor known in advance.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        // With no known proxy this is the whole defence: only the proxy's own entry is read.
        // The container must not be reachable past it.
        options.ForwardLimit = 1;
    });

const string apiReferencePath = "/scalar";

// Per address; the account lockout is the other half.
var loginWindow = new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) };

var refreshWindow = new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) };

var authWindow = new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

// Tied to the browser sink's one-upload-per-second cadence.
var clientLogWindow = new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) };

// Only the anonymous endpoints a caller can make expensive; nothing else, health probes included.
builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                // Before the prefix below, which would otherwise swallow both.
                if (context.Request.Path.StartsWithSegments(AuthRoute.LoginPath, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("login|" + ClientAddress(context), _ => loginWindow);

                if (context.Request.Path.StartsWithSegments(AuthRoute.RefreshPath, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("refresh|" + ClientAddress(context), _ => refreshWindow);

                if (context.Request.Path.StartsWithSegments(AuthRoute.Path, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("auth|" + ClientAddress(context), _ => authWindow);

                if (context.Request.Path.StartsWithSegments(ClientLogRoute.Path, StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("client-logs|" + ClientAddress(context), _ => clientLogWindow);

                return RateLimitPartition.GetNoLimiter("unlimited");
            });

        options.OnRejected = static async (context, _) =>
        {
            // Rounded up: truncation would answer a sub-second remainder with "Retry-After: 0".
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        };
    });

builder.Services.AddEvilCaseAuth();
builder.Services.AddEvilCaseFiles();

// The logger is passed explicitly: the parameterless overload does not register Serilog.ILogger.
builder.Host.UseSerilog(Log.Logger);

builder.Services.ConfigureServices();

builder.Services.AddHealthChecks().AddEvilCaseApiHealthChecks(HealthCheckTags.Ready);

var app = builder.Build();

// Off where the schema is rolled out separately or several instances start at once.
if (app.Configuration.GetValue("EvilBrains:EvilCase:Database:MigrateOnStartup", defaultValue: true))
{
    try
    {
        await app.MigrateEvilCaseDatabase(CancellationToken.None);
    }
    catch (Exception exception)
    {
        // Flushed here: the batching sinks would never ship this before the process dies.
        Log.Fatal(exception, "Database migration failed, the application will not start");
        await Log.CloseAndFlushAsync();

        throw;
    }
}

// After the migrations, before anything is served.
await app.SeedEvilCaseUser(CancellationToken.None);

// After the administrator: the sample data hangs on the tenant the seed creates.
if (app.Configuration.GetValue("EvilBrains:EvilCase:Database:SeedSampleData", defaultValue: false))
    await app.SeedEvilCaseSampleData(CancellationToken.None);

if (app.Configuration.GetValue("EvilBrains:EvilCase:Hosting:BehindReverseProxy", defaultValue: false))
    app.UseForwardedHeaders();

// Above the request logging, which logs the exception and rethrows.
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Scalar loads from a CDN with an inline script the CSP blocks. The carve-out holds
// only where Scalar is mapped; elsewhere /scalar is a frontend page.
app.UseWhen(
    context => !app.Environment.IsDevelopment()
        || !context.Request.Path.StartsWithSegments(apiReferencePath, StringComparison.OrdinalIgnoreCase),
    static branch => branch.UseMiddleware<SecurityHeadersMiddleware>());

// Quiet on the log upload: logging it would be shipped by the next upload.
app.UseRequestLogging(loggedPaths: ["/api"], quietPaths: [ClientLogRoute.Path]);

if (app.Configuration.GetValue("EvilBrains:EvilCase:Hosting:HttpsRedirection", defaultValue: true))
{
    // Probes usually arrive over plain HTTP; a redirect carries no body and counts as a failed probe.
    app.UseWhen(
        static context => !context.Request.Path.StartsWithSegments(HealthCheckPaths.Prefix, StringComparison.OrdinalIgnoreCase),
        static branch => branch.UseHttpsRedirection());
}

// So / hits the compressed static-asset endpoint; the fallback serves index.html uncompressed.
app.Use(
    static async (context, next) =>
    {
        if (context.Request.Path == "/")
            context.Request.Path = "/index.html";

        await next(context);
    });

app.UseRouting();

// Behind the forwarded headers, so a partition is the caller's address rather than the proxy's.
app.UseRateLimiter();

// Explicit: the builder's implicit UseAuthentication runs ahead of the whole pipeline,
// before forwarded headers and request logging.
app.UseAuthentication();
app.UseAuthorization();

// Replaces UseBlazorFrameworkFiles, whose /_framework branch would terminate ahead of it.
// Anonymous: the sign-in page needs its assets.
app.MapStaticAssets().AllowAnonymous();

app.MapEvilCaseApi();

if (app.Environment.IsDevelopment())
    app.MapEvilCaseApiReference();

app.MapFallbackToFile("index.html").AllowAnonymous();

await app.RunAsync();

// UseSerilog was handed a logger it does not own, so nothing else flushes it on a normal shutdown.
await Log.CloseAndFlushAsync();

static string ClientAddress(HttpContext context)
{
    return RateLimitPartitionKey.ForAddress(context.Connection.RemoteIpAddress);
}
