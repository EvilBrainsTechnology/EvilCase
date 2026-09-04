using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Logging;
using EvilBrains.Logging.WebAssembly;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabBlazor;

namespace EvilBrains.EvilCase.App;

// A named entry point rather than top-level statements: those compile to a Program in the global
// namespace, and the test project already has the host's.
internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.AddClientLogging("ClientLogging", "evilcase.machine-id", ClientLogRoute.Path);

        // One instance behind all three registrations.
        builder.Services.AddSingleton<IAccessTokenStore, AccessTokenStore>();
        builder.Services.AddSingleton<EvilCaseAuthenticationStateProvider>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(static services => services.GetRequiredService<EvilCaseAuthenticationStateProvider>());
        builder.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<EvilCaseAuthenticationStateProvider>());
        builder.Services.AddTransient<AuthTokenHandler>();

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddSingleton<IClientLogUploader, ApiLogUploader>();

        // The app is served by the API host, so the API is same-origin.
        builder.Services.AddEvilCaseApiClient(
            new Uri(builder.HostEnvironment.BaseAddress),
            static client => client.AddRequestContextHeaders().AddRequestLogging().AddHttpMessageHandler<AuthTokenHandler>());

        builder.Services.AddSingleton<IFileDownloader, FileDownloader>();

        // The same handler chain as the generated clients, so the upload and the download carry the same authorization.
        builder.Services
            .AddHttpClient<IFileTransferClient, FileTransferClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
            .AddRequestContextHeaders()
            .AddRequestLogging()
            .AddHttpMessageHandler<AuthTokenHandler>();

        builder.Services.AddTabBlazor(
            static options =>
            {
                options.EnablePopper = true;
                options.DefaultPositioning = Positioning.Absolute;

                // Default points at unpkg; popper.js is vendored in wwwroot instead.
                options.PopperScriptUrl = "lib/popper/popper.min.js";
            });

        var host = builder.Build();
        host.StartClientLogging();

        await host.RunAsync();
    }
}
