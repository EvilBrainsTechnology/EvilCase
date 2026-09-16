using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilesCardRenderTests
{
    [Test]
    public void BindingTheCardWideDropDoesNotLogAWarning()
    {
        using var ctx = new BunitContext();

        var logger = new CapturingLogger<FilesCard>();

        ctx.Services.AddSingleton<ILogger<FilesCard>>(logger);
        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var dropModule = ctx.JSInterop.SetupModule("./js/file-drop.js");
        dropModule.SetupVoid("bindCardDrop", static _ => true).SetVoidResult();

        var component = ctx.Render<FilesCard>(static parameters => parameters
            .Add(static card => card.LoadFiles, static (_) => Task.FromResult<IReadOnlyList<FileListItem>>([]))
            .Add(static card => card.UploadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        // OnAfterRenderAsync's own task is not awaited by the initial render (Blazor never blocks
        // a render on it), so the bind's completion is waited for explicitly here.
        component.WaitForAssertion(() => dropModule.VerifyInvoke("bindCardDrop"));

        Assert.That(logger.LoggedWarningOrAbove, Is.False, "bindCardDrop binds without a warning");
    }

    [Test]
    public void BindingTheCardWideDropCompletesAndUnbindsOnDispose()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var dropModule = ctx.JSInterop.SetupModule("./js/file-drop.js");
        dropModule.SetupVoid("bindCardDrop", static _ => true).SetVoidResult();
        dropModule.SetupVoid("unbindCardDrop", static _ => true).SetVoidResult();

        var component = ctx.Render<FilesCard>(static parameters => parameters
            .Add(static card => card.LoadFiles, static (_) => Task.FromResult<IReadOnlyList<FileListItem>>([]))
            .Add(static card => card.UploadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        // The bind must have completed (not just been called) before dispose runs, or the
        // dispose race guard treats it as a navigation away mid-bind and skips the unbind.
        component.WaitForAssertion(() => dropModule.VerifyInvoke("bindCardDrop"));

        ctx.DisposeComponentsAsync().GetAwaiter().GetResult();

        dropModule.VerifyInvoke("unbindCardDrop");
    }
}
