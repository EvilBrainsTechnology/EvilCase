using System.Net;
using System.Text;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class AuthTokenHandlerTests
{
    [Test]
    public async Task AnExpiringSessionIsRenewedBeforeTheRequestAndTheRequestCarriesNoToken()
    {
        var refresh = Renewing();

        using var response = await Send(refresh, ExpiringSession.Store(), new OkHandler(), "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.Refreshes, Is.EqualTo(1), "a request goes out on a session that has been renewed");
            Assert.That(response.RequestMessage?.Headers.Authorization, Is.Null, "the token travels as a cookie, never as a header");
            Assert.That(OptionsOf(response.RequestMessage!), Is.EqualTo(IncludingCredentials()), "fetch sends the cookies only when asked to");
        }
    }

    [Test]
    public async Task AnUnauthorizedRequestOnAFreshLookingSessionIsRenewedAndRetried()
    {
        var refresh = Renewing();
        var inner = new UnauthorizedOnceHandler();

        using var response = await Send(refresh, FreshSession(), inner, "/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.Refreshes, Is.EqualTo(1), "another tab may have cleared the cookies the session looks fresh on");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(inner.Sent, Has.Count.EqualTo(2));
            Assert.That(inner.Sent[^1].Body, Is.EqualTo(inner.Sent[0].Body), "the retry resends the original body");
            Assert.That(inner.Sent[^1].Options, Is.EqualTo(IncludingCredentials()), "the retry without credentials would leave the renewed cookies behind");
        }
    }

    [Test]
    public async Task AnUploadIsSentOnceAndNeverReadBeforehand()
    {
        var inner = new UnauthorizedOnceHandler();
        var file = new TrackedContent("the file");
        var checking = new ReadCheckingHandler(file, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/cases") { Content = new MultipartFormDataContent { { file, "file", "smlouva.pdf" } } };

        var refresh = Renewing();

        using var response = await Send(refresh, FreshSession(), checking, request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(checking.ReadBeforeSent, Is.False, "an upload is not held in memory for a retry");
            Assert.That(refresh.Refreshes, Is.EqualTo(1), "the next upload goes out on the renewed session");
            Assert.That(inner.Sent, Has.Count.EqualTo(1), "an upload has no copy to retry from");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }

    [Test]
    public async Task AnUnauthorizedRequestWhoseRenewalIsRefusedEndsTheSession()
    {
        var store = FreshSession();
        var inner = new UnauthorizedOnceHandler();

        using var response = await Send(new StubEarlyRefresh(refreshed: new EarlyRefreshResult(401, Body: null)), store, inner, "/api/cases");

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
        var refresh = new StubEarlyRefresh(refreshed: new EarlyRefreshResult(500, Body: null));

        using var response = await Send(refresh, ExpiringSession.Store(), new OkHandler(), ClientLogRoute.Path);

        Assert.That(refresh.Refreshes, Is.Zero, "a renewal logs when it fails, and that log is what the next upload ships");
    }

    private static StubEarlyRefresh Renewing()
    {
        return new StubEarlyRefresh(refreshed: StubEarlyRefresh.Renewed("user@example.com"));
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

    private static async Task<HttpResponseMessage> Send(StubEarlyRefresh refresh, SessionStore store, HttpMessageHandler inner, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost" + path) { Content = new StringContent("{\"title\":\"x\"}") };

        return await Send(refresh, store, inner, request);
    }

    private static async Task<HttpResponseMessage> Send(StubEarlyRefresh refresh, SessionStore store, HttpMessageHandler inner, HttpRequestMessage request)
    {
        using var session = new EvilCaseAuthenticationStateProvider(store, Substitute.For<IAuthClient>(), refresh, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
        await using var services = new ServiceCollection()
            .AddSingleton<IAuthSession>(session)
            .BuildServiceProvider();

        using var handler = new AuthTokenHandler(store, services) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);

        return await invoker.SendAsync(request, CancellationToken.None);
    }

    private sealed class TrackedContent(string text) : HttpContent
    {
        public bool Read { get; private set; }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            this.Read = true;

            await stream.WriteAsync(Encoding.UTF8.GetBytes(text));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            return false;
        }
    }

    /// <summary>
    /// Notes whether the body was read before it reached the network.
    /// </summary>
    private sealed class ReadCheckingHandler(TrackedContent body, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        public bool ReadBeforeSent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.ReadBeforeSent |= body.Read;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
