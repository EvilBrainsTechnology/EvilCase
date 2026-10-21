using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class AuthTokenHandlerTests
{
    [Test]
    public async Task AnExpiringSessionIsRenewedBeforeTheRequestAndTheRequestCarriesNoToken()
    {
        var authClient = RenewingAuthClient();

        using var response = await Send(authClient, ExpiringSession.Store(), new OkHandler(), "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(1).Items, "a request goes out on a session that has been renewed");
            Assert.That(response.RequestMessage?.Headers.Authorization, Is.Null, "the token travels as a cookie, never as a header");
            Assert.That(OptionsOf(response.RequestMessage!), Is.EqualTo(IncludingCredentials()), "fetch sends the cookies only when asked to");
        }
    }

    [Test]
    public async Task AnUnauthorizedRequestOnAFreshLookingSessionIsRenewedAndRetried()
    {
        var authClient = RenewingAuthClient();
        var inner = new UnauthorizedOnceHandler();

        using var response = await Send(authClient, FreshSession(), inner, "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(1).Items, "another tab may have cleared the cookies the session looks fresh on");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(inner.Sent, Has.Count.EqualTo(2));
            Assert.That(inner.Sent[^1].Body, Is.EqualTo(inner.Sent[0].Body), "the retry resends the original body");
            Assert.That(inner.Sent[^1].Options, Is.EqualTo(IncludingCredentials()), "the retry without credentials would leave the renewed cookies behind");
        }
    }

    [Test]
    public async Task AnUnauthorizedRequestWhoseRenewalIsRefusedEndsTheSession()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(HttpStatusCode.Unauthorized, responseBody: null));

        var store = FreshSession();
        var inner = new UnauthorizedOnceHandler();

        using var response = await Send(authClient, store, inner, "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(inner.Sent, Has.Count.EqualTo(1), "nothing is retried on a session the server refused");
            Assert.That(store.Current, Is.Null, "a tab whose cookies were cleared elsewhere signs out");
        }
    }

    [Test]
    public async Task AClientLogUploadNeverRenews()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        using var response = await Send(authClient, ExpiringSession.Store(), new OkHandler(), ClientLogRoute.Path);

        Assert.That(authClient.ReceivedCalls(), Is.Empty, "a renewal logs when it fails, and that log is what the next upload ships");
    }

    private static IAuthClient RenewingAuthClient()
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

        return authClient;
    }

    private static SessionStore FreshSession()
    {
        var store = new SessionStore();

        store.Set(new()
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Email = "user@example.com",
            Role = UserRole.User,
        });

        return store;
    }

    private static IDictionary<string, object?> IncludingCredentials()
    {
        using var request = new HttpRequestMessage();

        return OptionsOf(request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include));
    }

    private static IDictionary<string, object?> OptionsOf(HttpRequestMessage request)
    {
        return new Dictionary<string, object?>(request.Options);
    }

    private static async Task<HttpResponseMessage> Send(IAuthClient authClient, SessionStore store, HttpMessageHandler inner, string path)
    {
        using var session = new EvilCaseAuthenticationStateProvider(store, authClient, new StubEarlyRefresh(), TimeProvider.System, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
        await using var services = new ServiceCollection()
            .AddSingleton<IAuthSession>(session)
            .BuildServiceProvider();

        using var handler = new AuthTokenHandler(store, services) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost" + path) { Content = new StringContent("{\"title\":\"x\"}") };

        return await invoker.SendAsync(request, CancellationToken.None);
    }
}
