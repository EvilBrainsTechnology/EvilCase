using EvilCase.Api;
using EvilCase.Auth;
using EvilCase.Secrets;
using Scalar.AspNetCore;
using Serilog;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication
    .CreateBuilder(args);

builder.Configuration
    .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"AppSettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>() // TODO only if development?
    .AddEvilCaseSecrets(builder.Configuration, "EvilBrains:EvilCase:Infisical");

#pragma warning disable RCS0054
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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers();
app.Run();
