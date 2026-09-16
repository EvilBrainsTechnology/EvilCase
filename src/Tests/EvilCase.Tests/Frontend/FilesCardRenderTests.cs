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
    public void BindingTheCardWideDropDoesNotLogAWarningWhenTheModuleReturnsAProperObjectReference()
    {
        using var ctx = new BunitContext();

        var logger = new CapturingLogger<FilesCard>();

        ctx.Services.AddSingleton<ILogger<FilesCard>>(logger);
        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        // Mirrors bindCardDrop's fixed contract: file-drop.js hands back a real JS object
        // reference. Before the fix it returned a plain object, which bUnit's SetupModule (like
        // the real Blazor JS interop) refuses to accept as an IJSObjectReference.
        var dropModule = ctx.JSInterop.SetupModule("./js/file-drop.js");
        dropModule.SetupModule("bindCardDrop", static _ => true);

        ctx.Render<FilesCard>(static parameters => parameters
            .Add(static card => card.LoadFiles, static (_) => Task.FromResult<IReadOnlyList<FileListItem>>([]))
            .Add(static card => card.UploadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        Assert.That(logger.LoggedWarningOrAbove, Is.False, "a proper IJSObjectReference from bindCardDrop binds without a warning");
    }

    [Test]
    public void BindingTheCardWideDropCompletesAndUnbindsOnDisposeAcrossTheYieldedAwaits()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var dropModule = ctx.JSInterop.SetupModule("./js/file-drop.js");
        var binding = dropModule.SetupModule("bindCardDrop", static _ => true);
        binding.SetupVoid("dispose");

        ctx.Render<FilesCard>(static parameters => parameters
            .Add(static card => card.LoadFiles, static (_) => Task.FromResult<IReadOnlyList<FileListItem>>([]))
            .Add(static card => card.UploadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        // The bind still completes even though it now yields before each of its two JS calls.
        dropModule.VerifyInvoke("bindCardDrop");

        ctx.DisposeComponentsAsync().GetAwaiter().GetResult();

        binding.VerifyInvoke("dispose");
    }
}
