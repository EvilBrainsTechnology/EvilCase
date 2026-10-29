using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileVersionDeleteModalRenderTests
{
    [Test]
    public void TheConfirmationNamesTheVersionAndTheFile()
    {
        using var ctx = NewContext();

        var component = Render(ctx, static _ => Task.CompletedTask);

        Assert.That(
            component.Find(".ec-confirm-text").TextContent,
            Is.EqualTo("Opravdu smazat verzi 2 souboru a.pdf? Ostatní verze zůstanou. Akci nelze vrátit zpět."));
    }

    [Test]
    public async Task ConfirmingDeletesAndRaisesOnDeleted()
    {
        await using var ctx = NewContext();

        var calls = 0;
        var deleted = false;

        var component = Render(
            ctx,
            _ =>
            {
                calls++;

                return Task.CompletedTask;
            },
            () => deleted = true);

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(deleted, Is.True);
        }
    }

    [Test]
    public async Task AGoneVersionSaysSo()
    {
        await using var ctx = NewContext();

        var component = Render(ctx, static _ => Task.FromException(new ApiException(HttpStatusCode.NotFound, responseBody: null)));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("Verze už neexistuje.")));
    }

    [Test]
    public async Task TheOnlyVersionIsRefused()
    {
        await using var ctx = NewContext();

        var component = Render(ctx, static _ => Task.FromException(new ApiException(HttpStatusCode.Conflict, responseBody: null)));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("Jedinou verzi nejde smazat samostatně. Smažte celý soubor.")));
    }

    [Test]
    public async Task AFailedDeleteSaysSoAndLogs()
    {
        await using var ctx = NewContext();

        var logger = new CapturingLogger<FileVersionDeleteModal>();
        ctx.Services.AddSingleton<ILogger<FileVersionDeleteModal>>(logger);

        var deleted = false;
        var component = Render(ctx, static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)), () => deleted = true);

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("Verzi se nepodařilo smazat. Zkuste to za chvíli znovu.")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.False);
            Assert.That(logger.LoggedWarningOrAbove, Is.True);
        }
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private static IRenderedComponent<FileVersionDeleteModal> Render(BunitContext ctx, Func<CancellationToken, Task> delete, Action? onDeleted = null)
    {
        return ctx.Render<FileVersionDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.FileName, "a.pdf")
            .Add(static modal => modal.Number, 2)
            .Add(static modal => modal.Delete, delete)
            .Add(static modal => modal.OnDeleted, () => onDeleted?.Invoke()));
    }
}
