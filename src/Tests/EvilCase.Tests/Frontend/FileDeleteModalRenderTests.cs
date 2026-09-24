using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileDeleteModalRenderTests
{
    [Test]
    public void TheSentenceNamesTheFile()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.Render<FileDeleteModal>(static parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.FileName, "Rozhodnutí.pdf")
            .Add(static modal => modal.Delete, static _ => Task.CompletedTask));

        Assert.That(
            component.Find(".ec-confirm-text").TextContent,
            Does.Contain("Rozhodnutí.pdf").And.Contain("nelze vrátit zpět"));
    }

    [Test]
    public async Task ConfirmingCallsTheDeleteAndRaisesOnDeleted()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var calls = 0;
        var deleted = false;

        var component = ctx.Render<FileDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.FileName, "Rozhodnutí.pdf")
            .Add(
                static modal => modal.Delete,
                _ =>
                {
                    calls++;

                    return Task.CompletedTask;
                })
            .Add(static modal => modal.OnDeleted, () => deleted = true));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(deleted, Is.True);
        }
    }

    [Test]
    public async Task AFailedDeleteIsReportedInTheModal()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var deleted = false;

        var component = ctx.Render<FileDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.FileName, "Rozhodnutí.pdf")
            .Add(
                static modal => modal.Delete,
                static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)))
            .Add(static modal => modal.OnDeleted, () => deleted = true));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo smazat")));

        Assert.That(deleted, Is.False);
    }
}
