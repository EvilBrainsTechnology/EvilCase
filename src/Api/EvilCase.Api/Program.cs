using EvilBrains.EvilCase.Api;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Secrets;
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
    .AddEvilCaseSecrets(builder.Configuration, "EvilBrains:EvilCase:Infisical");

#pragma warning disable RCS0054 // Fix formatting of a call chain
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
#pragma warning restore RCS0054

builder.AddEvilCaseAuth("EvilBrains:EvilCase:Auth");

builder.Host.UseSerilog();

builder.Services.ConfigureServices();

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

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.Run();
