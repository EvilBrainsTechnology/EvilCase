using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App;
using EvilBrains.EvilCase.App.Logging;
using EvilBrains.Logging.WebAssembly;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing");

builder.AddClientLogging("ClientLogging", "evilcase.machine-id", nameof(ILogsClient));

builder.Services.AddSingleton<IClientLogUploader, ApiLogUploader>();
builder.Services.AddEvilCaseApiClient(new Uri(apiBaseUrl), client => client.AddRequestContextHeaders());

builder.Services.AddTabBlazor(
    options =>
    {
        options.EnablePopper = true;
        options.DefaultPositioning = Positioning.Absolute;

        // Default points at unpkg; popper.js is vendored in wwwroot instead.
        options.PopperScriptUrl = "lib/popper/popper.min.js";
    });

var host = builder.Build();
host.StartClientLogging();

await host.RunAsync();
