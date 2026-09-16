using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilesCardRenderTests
{
    [Test]
    public void FilesHandedToTheHiddenInputUploadAndReloadTheList()
    {
        using var ctx = new BunitContext();

        // FileDropZone imports its own module from a path carrying a version query string, which
        // no SetupModule can name, and reads a property bUnit's runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var logger = new CapturingLogger<FilesCard>();
        var uploaded = new List<string>();
        var loads = 0;

        ctx.Services.AddSingleton<ILogger<FilesCard>>(logger);
        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var component = ctx.Render<FilesCard>(parameters => parameters
            .Add(
                static card => card.LoadFiles,
                _ =>
                {
                    loads++;

                    return Task.FromResult<IReadOnlyList<FileListItem>>([]);
                })
            .Add(
                static card => card.UploadFile,
                (file, _) =>
                {
                    uploaded.Add(file.Name);

                    return Task.CompletedTask;
                })
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        component.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "a.txt"),
            InputFileContent.CreateFromText("b", "b.txt"));

        // The reload is the last step of the batch, so waiting for it waits for the uploads too.
        component.WaitForAssertion(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo(["a.txt", "b.txt"]), "a card-wide drop uploads every dropped file, in order");
            Assert.That(logger.LoggedWarningOrAbove, Is.False, "an upload that succeeds logs nothing");
        }
    }

    [Test]
    public async Task ADropCarryingNoFileUploadsNothingAndShowsNoError()
    {
        await using var ctx = new BunitContext();

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var uploaded = new List<string>();
        var loads = 0;

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var component = ctx.Render<FilesCard>(parameters => parameters
            .Add(
                static card => card.LoadFiles,
                _ =>
                {
                    loads++;

                    return Task.FromResult<IReadOnlyList<FileListItem>>([]);
                })
            .Add(
                static card => card.UploadFile,
                (file, _) =>
                {
                    uploaded.Add(file.Name);

                    return Task.CompletedTask;
                })
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        // Dropping dragged text or a link hands the input an empty file list, which the drop zone
        // reports like any other change. UploadFiles refuses to raise an empty one, so the input's
        // own OnChange is raised instead.
        var input = component.FindComponent<InputFile>();
        var change = new InputFileChangeEventArgs([]);

        await component.InvokeAsync(async () => await input.Instance.OnChange.InvokeAsync(change));

        await component.WaitForAssertionAsync(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the empty batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.Empty, "a drop carrying no file starts no upload");
            Assert.That(component.FindAll(".alert-danger"), Is.Empty, "a drop carrying no file reports no failure");
        }
    }

    [Test]
    public void TheHiddenFileInputSitsInsideTheDropZone()
    {
        using var ctx = new BunitContext();

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var component = ctx.Render<FilesCard>(static parameters => parameters
            .Add(static card => card.LoadFiles, static (_) => Task.FromResult<IReadOnlyList<FileListItem>>([]))
            .Add(static card => card.UploadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DownloadFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
            .Add(static card => card.OwnerGoneError, "spis už neexistuje."));

        // The drop zone hands a drop to the first file input inside its own div and nowhere else.
        var dropZone = component.Find("div.card.ec-dropcard");

        Assert.That(dropZone.QuerySelector("input[type=file]"), Is.Not.Null, "the drop target holds the input a drop is handed to");
    }
}
