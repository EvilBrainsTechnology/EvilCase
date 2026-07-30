using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing");

builder.Services.AddEvilCaseApiClient(new Uri(apiBaseUrl));

builder.Services.AddTabBlazor(
    options =>
    {
        options.EnablePopper = true;
        options.DefaultPositioning = Positioning.Absolute;

        // Default points at unpkg; popper.js is vendored in wwwroot instead.
        options.PopperScriptUrl = "lib/popper/popper.min.js";
    });

await builder.Build().RunAsync();
