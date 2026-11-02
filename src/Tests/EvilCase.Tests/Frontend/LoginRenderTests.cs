using Bunit;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LoginRenderTests
{
    [Test]
    public async Task TheSignInFormRenders()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession());

        var component = ctx.Render<Login>();

        Assert.That(component.FindComponents<EditForm>(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AReturnUrlOutsideTheApplicationIsIgnored()
    {
        var inside = await SignInReturningTo("/cases");
        var outside = await SignInReturningTo("//evil.example");
        var backslashed = await SignInReturningTo("/\\evil.example/");
        var tabbed = await SignInReturningTo("/\t/evil.example/");
        var absolute = await SignInReturningTo("https://evil.example/");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inside.Uri, Does.EndWith("/cases"));
            Assert.That(
                outside.Uri,
                Is.EqualTo(outside.BaseUri),
                "a protocol-relative target leads out of the application (SDD-016)");
            Assert.That(
                backslashed.Uri,
                Is.EqualTo(backslashed.BaseUri),
                "the browser reads a backslash after the leading slash as a second slash (SDD-016)");
            Assert.That(tabbed.Uri, Is.EqualTo(tabbed.BaseUri), "the browser drops a tab between the two slashes (SDD-016)");
            Assert.That(absolute.Uri, Is.EqualTo(absolute.BaseUri), "an absolute target on another host leads out of the application (SDD-016)");
        }
    }

    [Test]
    public async Task AnInvalidSubmitClearsTheFailedSignIn()
    {
        await using var ctx = FrontendContext.Create();

        ctx.Services.GetRequiredService<StubAuthSession>().Outcome = SignInOutcome.InvalidCredentials;

        var component = ctx.Render<Login>();

        await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "spravce@example.cz" });
        await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "SpatneHeslo1" });
        await component.Find("button[type=\"submit\"]").ClickAsync(new MouseEventArgs());

        var failed = component.FindAll(".ec-alert").Count;

        await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "" });
        await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "" });
        await component.Find("button[type=\"submit\"]").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failed, Is.EqualTo(1));
            Assert.That(component.FindAll(".ec-alert"), Is.Empty, "an invalid submit drops the error of the previous sign-in");
        }
    }

    private static async Task<(string Uri, string BaseUri)> SignInReturningTo(string returnUrl)
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession { Outcome = SignInOutcome.Success });

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();

        // The page reads the target from the query string, the only way a value reaches
        // a SupplyParameterFromQuery parameter.
        navigation.NavigateTo(navigation.GetUriWithQueryParameter(RedirectToLogin.ReturnUrlParameter, returnUrl));

        var component = ctx.Render<Login>();

        await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "spravce@example.cz" });
        await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "SpravneHeslo1" });
        await component.Find("button[type=\"submit\"]").ClickAsync(new MouseEventArgs());

        return (navigation.Uri, navigation.BaseUri);
    }
}
