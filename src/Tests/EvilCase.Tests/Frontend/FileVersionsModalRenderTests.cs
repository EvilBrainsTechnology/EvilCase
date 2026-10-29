using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileVersionsModalRenderTests
{
    private const string Uploader = "jan.priklad@example.cz";

    private static readonly DateTime FirstCreated = new(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void TheVersionsGoFromTheNewestAndOnlyTheCurrentIsMarked()
    {
        using var ctx = NewContext();

        var component = Render(ctx);
        var rows = component.FindAll(".ec-versions-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows.Select(static row => Normalize(row.QuerySelector(".ec-versions-title")!.TextContent)), Is.EqualTo(["Verze 3 aktuální", "Verze 2", "Verze 1"]));
            Assert.That(component.FindAll(".ec-badge-current"), Has.Count.EqualTo(1));
            Assert.That(rows[0].QuerySelector(".ec-badge-current"), Is.Not.Null);
            Assert.That(
                Normalize(rows[0].QuerySelector(".ec-versions-meta")!.TextContent),
                Is.EqualTo($"{MomentDisplay.Text(FirstCreated.AddDays(2))} · {Uploader} · {FileSizeDisplay.Text(3072)}"));
            Assert.That(Normalize(component.Find(".ec-versions-subtitle").TextContent), Is.EqualTo("a.pdf · 3 verze"));
        }
    }

    [TestCase(1, "1 verze")]
    [TestCase(4, "4 verze")]
    [TestCase(5, "5 verzí")]
    public void TheSubtitleCountsTheVersionsInCzech(int count, string expected)
    {
        using var ctx = NewContext();

        var component = Render(ctx, file: File(count, count));

        Assert.That(Normalize(component.Find(".ec-versions-subtitle").TextContent), Is.EqualTo($"a.pdf · {expected}"));
    }

    [Test]
    public void TheOnlyVersionOffersNoDelete()
    {
        using var ctx = NewContext();

        var component = Render(ctx, file: File(1, 1), loadVersions: static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([Item(1)]));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("[aria-label='Smazat verzi 1']"), Is.Empty);
            Assert.That(component.FindAll("[aria-label='Stáhnout verzi 1']"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task DeletingAVersionAsksFirstThenDeletesAndRaisesOnChanged()
    {
        await using var ctx = NewContext();

        var deleted = new List<int>();
        var changed = 0;
        var component = Render(
            ctx,
            deleteVersion: (_, number, _) =>
            {
                deleted.Add(number);

                return Task.CompletedTask;
            },
            onChanged: () => changed++);

        await component.Find("[aria-label='Smazat verzi 2']").ClickAsync(new MouseEventArgs());

        Assert.That(deleted, Is.Empty, "the confirmation comes first");

        await component.FindAll("dialog .ec-modal-footer .ec-button-danger").Single(static button => button.TextContent.Contains("Smazat verzi", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo([2]));
            Assert.That(changed, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task PreviewAndDownloadRaiseTheNumber()
    {
        await using var ctx = NewContext();

        var previewed = new List<int>();
        var downloaded = new List<int>();
        var component = Render(ctx, onPreview: previewed.Add, onDownload: downloaded.Add);

        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-preview button")!.ClickAsync(new MouseEventArgs());
        await component.Find("[aria-label='Stáhnout verzi 1']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previewed, Is.EqualTo([2]));
            Assert.That(downloaded, Is.EqualTo([1]));
        }
    }

    [Test]
    public async Task TheWholeRowOpensThePreview()
    {
        await using var ctx = NewContext();

        var previewed = new List<int>();
        var component = Render(ctx, onPreview: previewed.Add);

        await component.FindAll(".ec-versions-row")[2].QuerySelector(".ec-versions-main")!.ClickAsync(new MouseEventArgs());

        Assert.That(previewed, Is.EqualTo([1]));
    }

    [Test]
    public async Task DeleteFileRaisesOnDeleteFile()
    {
        await using var ctx = NewContext();

        var raised = 0;
        var component = Render(ctx, onDeleteFile: () => raised++);

        await component.Find(".ec-versions-delete-file button").ClickAsync(new MouseEventArgs());

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public async Task AFailedLoadOffersARetry()
    {
        await using var ctx = NewContext();

        var calls = 0;
        var component = Render(
            ctx,
            loadVersions: async (_, _) =>
            {
                calls++;

                return calls == 1 ? throw new HttpRequestException() : await Task.FromResult(Versions());
            });

        Assert.That(component.Find(".ec-card-error .ec-alert").TextContent, Is.EqualTo("Verze se nepodařilo načíst. Zkuste to za chvíli znovu."));

        await component.Find(".ec-card-error button").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-versions-row"), Has.Count.EqualTo(3)));
    }

    [Test]
    public async Task ANewVersionIsUploadedAndItsNoticeShown()
    {
        await using var ctx = NewContext();

        var uploaded = new List<(string File, string Picked)>();
        var component = Render(
            ctx,
            uploadVersion: (file, picked) =>
            {
                uploaded.Add((file.FileName, picked.Name));

                return Task.FromResult<(string?, string?)>((null, "a.pdf: nahráno jako verze 4."));
            });

        component.FindComponent<FileVersionPicker>().FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "b.pdf"));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-file-notice").TextContent, Is.EqualTo("a.pdf: nahráno jako verze 4.")));

        Assert.That(uploaded, Is.EqualTo([("a.pdf", "b.pdf")]));
    }

    [Test]
    public async Task AFailedNewVersionShowsItsReason()
    {
        await using var ctx = NewContext();

        var component = Render(
            ctx,
            uploadVersion: static (_, _) => Task.FromResult<(string?, string?)>(("b.docx: nová verze souboru a.pdf musí mít příponu .pdf.", null)));

        component.FindComponent<FileVersionPicker>().FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "b.docx"));

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("b.docx: nová verze souboru a.pdf musí mít příponu .pdf.")));
    }

    [Test]
    public void TheWindowIsASheet()
    {
        using var ctx = NewContext();

        var component = Render(ctx);

        Assert.That(component.Find("dialog").ClassList, Does.Contain("ec-modal-sheet"));
    }

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private static FileListItem File(int version, int count)
    {
        return new FileListItem { FileId = Guid.NewGuid(), FileName = "a.pdf", SizeBytes = 3072, Created = FirstCreated, Version = version, VersionCount = count };
    }

    private static FileVersionItem Item(int number)
    {
        return new FileVersionItem { Number = number, SizeBytes = number * 1024L, Created = FirstCreated.AddDays(number - 1), UploaderEmail = Uploader };
    }

    private static IReadOnlyList<FileVersionItem> Versions()
    {
        return [Item(3), Item(2), Item(1)];
    }

    private static IRenderedComponent<FileVersionsModal> Render(
        BunitContext ctx,
        FileListItem? file = null,
        Func<FileListItem, CancellationToken, Task<IReadOnlyList<FileVersionItem>>>? loadVersions = null,
        Func<FileListItem, int, CancellationToken, Task>? deleteVersion = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<int>? onPreview = null,
        Action<int>? onDownload = null,
        Action? onDeleteFile = null,
        Action? onChanged = null)
    {
        var shown = file ?? File(3, 3);
        var load = loadVersions ?? (static (_, _) => Task.FromResult(Versions()));
        var delete = deleteVersion ?? (static (_, _, _) => Task.CompletedTask);
        var upload = uploadVersion ?? (static (_, _) => Task.FromResult<(string?, string?)>((null, null)));

        return ctx.Render<FileVersionsModal>(parameters => parameters
            .Add(static modal => modal.File, shown)
            .Add(static modal => modal.LoadVersions, load)
            .Add(static modal => modal.DeleteVersion, delete)
            .Add(static modal => modal.UploadVersion, upload)
            .Add(static modal => modal.OnPreview, number => onPreview?.Invoke(number))
            .Add(static modal => modal.OnDownload, number => onDownload?.Invoke(number))
            .Add(static modal => modal.OnDeleteFile, () => onDeleteFile?.Invoke())
            .Add(static modal => modal.OnChanged, () => onChanged?.Invoke())
            .Add(static modal => modal.Open, value: true));
    }
}
