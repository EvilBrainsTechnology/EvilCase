using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.App.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class AuthTokenHandlerTests
{
    [Test]
    public async Task AnExpiringTokenIsRenewedBeforeTheRequest()
    {
        var refreshes = await RefreshesFor("/api/cases");

        Assert.That(refreshes, Is.EqualTo(1), "a request goes out with a token that has been renewed");
    }

    [Test]
    public async Task AClientLogUploadNeverRenews()
    {
        var refreshes = await RefreshesFor(ClientLogRoute.Path);

        Assert.That(refreshes, Is.Zero, "a renewal logs when it fails, and that log is what the next upload ships");
    }

    private static async Task<int> RefreshesFor(string path)
    {
        var tokens = ExpiringSession.Store();
        var authClient = new FakeAuthClient(new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        using var session = new EvilCaseAuthenticationStateProvider(tokens, authClient, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
        await using var services = new ServiceCollection()
            .AddSingleton<IAuthSession>(session)
            .BuildServiceProvider();

        using var handler = new AuthTokenHandler(tokens, services) { InnerHandler = new OkHandler() };
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost" + path);
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        return authClient.Refreshes;
    }
}
