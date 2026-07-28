using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing");

builder.Services.AddEvilCaseApiClient(new Uri(apiBaseUrl));

await builder.Build().RunAsync();
