using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ConfirmModalRenderTests
{
    [TestCase(HttpStatusCode.Conflict, "Kontakt figuruje ve spisu.")]
    [TestCase(HttpStatusCode.NotFound, "Kontakt už neexistuje.")]
    [TestCase(HttpStatusCode.InternalServerError, "Kontakt se nepodařilo smazat.")]
    public async Task AFailedActionShowsThePageTextBeforeTheCommonOnes(HttpStatusCode status, string expected)
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.Render<ConfirmModal>(parameters => parameters
            .Add(static modal => modal.Title, "Smazat kontakt")
            .Add(static modal => modal.ConfirmText, "Smazat kontakt")
            .Add(static modal => modal.ChildContent, "Opravdu smazat kontakt?")
            .Add(static modal => modal.Action, _ => throw new ApiException(status, responseBody: null))
            .Add(static modal => modal.NotFoundText, "Kontakt už neexistuje.")
            .Add(static modal => modal.FailureText, "Kontakt se nepodařilo smazat.")
            .Add(
                static modal => modal.ErrorText,
                static exception => exception.StatusCode == HttpStatusCode.Conflict ? "Kontakt figuruje ve spisu." : null)
            .Add(static modal => modal.Open, value: true));

        await component.Find(".ec-modal-footer button.ec-button-danger").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("[role=alert]").TextContent, Is.EqualTo(expected));
    }
}
