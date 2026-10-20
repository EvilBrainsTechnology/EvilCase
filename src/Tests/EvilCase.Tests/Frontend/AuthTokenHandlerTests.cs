using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class AuthTokenHandlerTests
{
    [Test]
    public async Task AnExpiringSessionIsRenewedBeforeTheRequestAndTheRequestCarriesNoToken()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .Returns(new LoginResponse
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Email = "user@example.com",
                Role = UserRole.User,
            });

        using var response = await Send(authClient, "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(1).Items, "a request goes out on a session that has been renewed");
            Assert.That(response.RequestMessage?.Headers.Authorization, Is.Null, "the token travels as a cookie, never as a header");
        }
    }

    [Test]
    public async Task AClientLogUploadNeverRenews()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        using var response = await Send(authClient, ClientLogRoute.Path);

        Assert.That(authClient.ReceivedCalls(), Is.Empty, "a renewal logs when it fails, and that log is what the next upload ships");
    }

    private static async Task<HttpResponseMessage> Send(IAuthClient authClient, string path)
    {
        var tokens = ExpiringSession.Store();

        using var session = new EvilCaseAuthenticationStateProvider(tokens, authClient, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
        await using var services = new ServiceCollection()
            .AddSingleton<IAuthSession>(session)
            .BuildServiceProvider();

        using var handler = new AuthTokenHandler(tokens, services) { InnerHandler = new OkHandler() };
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost" + path);

        return await invoker.SendAsync(request, CancellationToken.None);
    }
}
