using EvilBrains.EvilCase.Api;
using EvilBrains.EvilCase.Auth;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using EvilBrains.Secrets.Infisical;
using Scalar.AspNetCore;
using Serilog;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication
    .CreateBuilder(args);

// The default appsettings.json/appsettings.{Environment}.json/user-secrets sources are
// added by CreateBuilder in the correct precedence slot (before environment variables
// and command-line arguments); only Infisical is appended so its secrets always win.
builder.Configuration
    .AddInfisicalSecrets(builder.Configuration, "EvilBrains:EvilCase:Infisical");

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

builder.Services.AddCors(options => options.AddPolicy(
    "App",
    policy => policy
        .WithOrigins("https://localhost:5001")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseRequestLogging("/logs/client");

app.UseHttpsRedirection();
app.UseRouting();

if (app.Environment.IsDevelopment())
    app.UseCors("App");

app.UseAuthorization();

app.MapControllers();
await app.RunAsync();
