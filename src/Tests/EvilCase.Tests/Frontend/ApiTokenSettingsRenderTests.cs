using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.ApiTokens;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ApiTokenSettingsRenderTests
{
    private const string Secret = "evc_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static readonly ApiTokenItem Live = new()
    {
        ApiTokenId = Guid.CreateVersion7(),
        Name = "Claude Code",
        Scope = ApiTokenScope.ReadWrite,
        Created = DateTime.UtcNow.AddDays(-1),
        Expires = DateTime.UtcNow.AddDays(89),
        LastUsed = DateTime.UtcNow.AddMinutes(-5),
    };

    private static readonly ApiTokenItem Expired = new()
    {
        ApiTokenId = Guid.CreateVersion7(),
        Name = "Zkušební skript",
        Scope = ApiTokenScope.ReadOnly,
        Created = DateTime.UtcNow.AddDays(-40),
        Expires = DateTime.UtcNow.AddDays(-10),
    };

    [Test]
    public void TheApiTokenSettingsRender()
    {
        using var ctx = Context(out _);

        var component = ctx.Render<ApiTokenSettings>();

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-token-row"), Has.Count.EqualTo(3)));
    }

    [Test]
    public void OnlyAnExpiredTokenIsMarkedExpired()
    {
        using var ctx = Context(out _);

        var component = ctx.Render<ApiTokenSettings>();
        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-settings-list .ec-token-row"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-settings-list .ec-token-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].QuerySelectorAll(".ec-token-expired"), Is.Empty, "a token before its expiry is live");
            Assert.That(rows[1].QuerySelectorAll(".ec-token-expired"), Is.Not.Empty, "a token past its expiry shows Vypršel");
        }
    }

    [Test]
    public async Task TheCreatedTokenIsShownUntilTheWindowCloses()
    {
        await using var ctx = Context(out var client);

        client
            .CreateApiToken(Arg.Any<ApiTokenCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ApiTokenCreated { Token = Live, Secret = Secret });

        var component = ctx.Render<ApiTokenSettings>();
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-token-row"), Has.Count.EqualTo(3)));

        await component.Find(".ec-card-header-actions .ec-button").ClickAsync(new MouseEventArgs());
        await component.Find("#api-token-name").ChangeAsync(new ChangeEventArgs { Value = "Claude Code" });
        await component.FindAll(".ec-modal-footer .ec-button")[^1].ClickAsync(new MouseEventArgs());

        var shown = component.Find("#api-token-secret").GetAttribute("value");

        await component.FindAll(".ec-modal-footer .ec-button")[^1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(shown, Is.EqualTo(Secret));
            Assert.That(component.Markup, Does.Not.Contain(Secret), "the secret is gone once its window closes");
        }
    }

    private static BunitContext Context(out IApiTokensClient client)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        client = Substitute.For<IApiTokensClient>();
        client
            .ListApiTokens(Arg.Any<CancellationToken>())
            .Returns(new ApiTokenListResponse { Items = [Live, Expired] });

        ctx.Services.AddSingleton(client);
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());

        return ctx;
    }
}
