using Bunit;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LoginRenderTests
{
    [Test]
    public async Task TheFormIsBuiltFromTheEcPrimitives()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession());

        var component = ctx.Render<Login>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-field"), Has.Count.EqualTo(2));
            Assert.That(component.Find("label[for=\"login-email\"]").TextContent.Trim(), Does.StartWith("E-mail"));
            Assert.That(component.Find("label[for=\"login-password\"]").TextContent.Trim(), Does.StartWith("Heslo"));
            Assert.That(component.Find("button[type=\"submit\"]").ClassList, Does.Contain("ec-button").And.Contain("ec-button-primary"));
            Assert.That(
                component.FindAll(".btn, .form-control, .card, .alert, .container-tight"),
                Is.Empty,
                "the sign-in page carries no Tabler class (SDD-020)");
        }
    }

    [Test]
    public async Task ARefusedSignInShowsTheReasonInsideTheForm()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession { Outcome = SignInOutcome.InvalidCredentials });

        var component = ctx.Render<Login>();

        await component.Find("#login-email").ChangeAsync(new ChangeEventArgs { Value = "spravce@example.cz" });
        await component.Find("#login-password").ChangeAsync(new ChangeEventArgs { Value = "ChybneHeslo1" });
        await component.Find("button[type=\"submit\"]").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("Nesprávný e-mail nebo heslo."));

        var children = component.Find("form").Children.ToList();
        var lastFieldIndex = children.FindLastIndex(static c => c.ClassList.Contains("ec-field"));
        var alertIndex = children.FindIndex(static c => c.ClassList.Contains("ec-alert"));
        var submitIndex = children.FindIndex(static c => c.ClassList.Contains("ec-button"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(alertIndex, Is.GreaterThan(lastFieldIndex), "a refused sign-in is answered where the form was filled in");
            Assert.That(alertIndex, Is.LessThan(submitIndex), "a refused sign-in is answered where the form was filled in");
        }
    }

    [Test]
    public async Task AReturnUrlOutsideTheApplicationIsIgnored()
    {
        var inside = await SignInReturningTo("/cases");
        var outside = await SignInReturningTo("//evil.example");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inside.Uri, Does.EndWith("/cases"));
            Assert.That(
                outside.Uri,
                Is.EqualTo(outside.BaseUri),
                "a protocol-relative target leads out of the application (SDD-016)");
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
