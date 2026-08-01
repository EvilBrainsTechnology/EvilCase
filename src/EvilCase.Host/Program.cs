using DotNetEnv;
using EvilBrains.EvilCase.Api;
using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
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
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty(AppSource.PropertyName, AppSource.Server)
    .CreateLogger();
#pragma warning restore RCS0054

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

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Only the API is logged: the frontend, its assets and the health probes would otherwise bury it.
// The log upload is the exception inside it — logging it would be shipped by the next upload.
app.UseRequestLogging(loggedPaths: ["/api"], quietPaths: ["/api/logs/client"]);

// Probes usually arrive over plain HTTP; a redirect carries no body and counts as a failed probe.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments(HealthCheckPaths.Prefix, StringComparison.OrdinalIgnoreCase),
    branch => branch.UseHttpsRedirection());

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapEvilCaseApi();

if (app.Environment.IsDevelopment())
    app.MapEvilCaseApiReference();

app.MapFallbackToFile("index.html");

await app.RunAsync();

// UseSerilog was handed a logger it does not own, so nothing else flushes it on a normal shutdown.
await Log.CloseAndFlushAsync();
