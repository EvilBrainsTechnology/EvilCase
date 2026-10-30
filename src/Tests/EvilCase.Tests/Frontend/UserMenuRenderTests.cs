using Bunit;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class UserMenuRenderTests
{
    [TestCase("Odhlásit se", false)]
    [TestCase("Odhlásit všechna zařízení", true)]
    public async Task ASignOutItemSignsOutItsScopeAndGoesToTheSignInPage(string item, bool everywhere)
    {
        await using var ctx = new BunitContext();
        var session = new StubAuthSession();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IAuthSession>(session);
        ctx.AddAuthorization().SetAuthorized("spravce@example.cz");

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<UserMenu>();

        await component.Find(".ec-avatar").ClickAsync(new MouseEventArgs());
        await component
            .FindAll("[role=menuitem]")
            .Single(button => string.Equals(button.TextContent.Trim(), item, StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.SignOuts.Single(), Is.EqualTo(everywhere), "each item signs out its own scope");
            Assert.That(navigation.Uri, Does.EndWith("/login"));
        }
    }
}
