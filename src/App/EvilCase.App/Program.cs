using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App;
using EvilBrains.EvilCase.App.Http;
using EvilBrains.EvilCase.App.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Filters;
using TabBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing");

var loggingOptions = builder.Configuration.GetSection(ClientLoggingOptions.SectionName).Get<ClientLoggingOptions>() ?? new();
var apiLogSink = new ApiLogSink();

SelfLog.Enable(message => Console.Error.WriteLine(message));

#pragma warning disable RCS0054 // Fix formatting of a call chain

// Shipping a batch is itself an HTTP request, so without this filter the sink would feed itself.
Action<LoggerConfiguration> shipToApi = shipped => shipped
    .Filter.ByExcluding(Matching.FromSource("System.Net.Http.HttpClient"))
    .WriteTo.Sink(apiLogSink, loggingOptions.ServerMinimumLevel);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(loggingOptions.MinimumLevel)
    .MinimumLevel.Override($"System.Net.Http.HttpClient.{nameof(ILogsClient)}", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.Logger(shipToApi)
    .CreateLogger();
#pragma warning restore RCS0054

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Services.AddSingleton<IClientIdentity, ClientIdentity>();
builder.Services.AddTransient<RequestContextHandler>();
builder.Services.AddEvilCaseApiClient(new Uri(apiBaseUrl), client => client.AddHttpMessageHandler<RequestContextHandler>());

builder.Services.AddTabBlazor(
    options =>
    {
        options.EnablePopper = true;
        options.DefaultPositioning = Positioning.Absolute;

        // Default points at unpkg; popper.js is vendored in wwwroot instead.
        options.PopperScriptUrl = "lib/popper/popper.min.js";
    });

var host = builder.Build();

apiLogSink.Start(host.Services.GetRequiredService<ILogsClient>(), host.Services.GetRequiredService<NavigationManager>());

await host.RunAsync();
