using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.App;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Logging;
using EvilBrains.Logging.WebAssembly;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.AddClientLogging("ClientLogging", "evilcase.machine-id", ClientLogRoute.Path);

// The access token never leaves memory, so a reload starts signed out and the refresh cookie is what
// puts the user back. One instance carries the session, the router's view of it and the renewal lock.
builder.Services.AddSingleton<IAccessTokenStore, AccessTokenStore>();
builder.Services.AddSingleton<EvilCaseAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(services => services.GetRequiredService<EvilCaseAuthenticationStateProvider>());
builder.Services.AddSingleton<IAuthSession>(services => services.GetRequiredService<EvilCaseAuthenticationStateProvider>());
builder.Services.AddTransient<AuthTokenHandler>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// The app is served by the API host, so the API is same-origin.
builder.Services.AddSingleton<IClientLogUploader, ApiLogUploader>();
builder.Services.AddEvilCaseApiClient(
    new Uri(builder.HostEnvironment.BaseAddress),
    client => client.AddRequestContextHeaders().AddRequestLogging().AddHttpMessageHandler<AuthTokenHandler>());

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
