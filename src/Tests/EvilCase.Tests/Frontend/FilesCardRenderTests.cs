using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilesCardRenderTests
{
    [Test]
    public async Task ADropDuringAnUploadGoesToAFreshInputAndIsRejected()
    {
        await using var ctx = FrontendContext.Create();

        var gate = new TaskCompletionSource();
        var uploaded = new List<string>();
        var loads = 0;

        var component = new Card
        {
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([]);
            },
            UploadFile = async (file, _) =>
            {
                uploaded.Add(file.Name);
                await (string.Equals(file.Name, "a.txt", StringComparison.Ordinal) ? gate.Task : Task.CompletedTask);

                return Stored(file.Name);
            },
        }
            .Render(ctx);

        var first = component.FindComponent<InputFile>().Instance;
        var firstBatch = component.InvokeAsync(async () => await first.OnChange.InvokeAsync(Change("a.txt", "b.txt")));

        // InvokeAsync queues behind work the renderer has not finished, so the change can run after it returns.
        await component.WaitForAssertionAsync(() => Assert.That(uploaded, Is.EqualTo(["a.txt"])));

        var inputs = component.FindComponents<InputFile>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs, Has.Count.EqualTo(3));
            Assert.That(inputs[1].Instance, Is.SameAs(first), "a change empties the input's file list, so a batch keeps its own input, beside the version input");
            Assert.That(component.Find("label.ec-file-upload").GetAttribute("for"), Is.EqualTo(component.FindAll("input[type=file]")[0].Id), "the next pick goes to the fresh input");
        }

        await Drop(component);

        Assert.That(component.FindAll(".ec-alert"), Is.Empty, "dragged text or a paste carries no file and asks nothing of the user");

        await Drop(component, "c.txt");
        await Drop(component, "d.txt");

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-alert").Select(static alert => alert.TextContent), Is.EqualTo(["Počkejte, až se dokončí nahrávání."]), "a repeated drop asks to wait once"));

        gate.SetResult();
        await firstBatch;

        await component.WaitForAssertionAsync(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo(["a.txt", "b.txt"]), "a drop during an upload uploads nothing and leaves the batch in flight whole");
            Assert.That(component.FindComponents<InputFile>(), Has.Count.EqualTo(2), "a finished batch takes its input with it, beside the version input");
            Assert.That(component.FindAll(".ec-file-uploading"), Is.Empty);
        }
    }

    [Test]
    public async Task ADropCarryingNoFileUploadsNothingAndShowsNoError()
    {
        await using var ctx = FrontendContext.Create();

        var uploaded = new List<string>();
        var loads = 0;

        var component = new Card
        {
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([]);
            },
            UploadFile = (file, _) =>
            {
                uploaded.Add(file.Name);

                return Task.FromResult(Stored(file.Name));
            },
        }
            .Render(ctx);

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
            Assert.That(component.FindAll(".ec-alert"), Is.Empty, "a drop carrying no file reports no failure");
        }
    }

    [Test]
    public void AFinishedBatchLeavesAFreshInputBehind()
    {
        using var ctx = FrontendContext.Create();

        var component = new Card().Render(ctx);

        var before = component.FindComponent<InputFile>().Instance;

        component.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a", "a.txt"));

        component.WaitForAssertion(() => Assert.That(
            component.FindComponent<InputFile>().Instance,
            Is.Not.SameAs(before),
            "a browser raises no change event for a file already in the input, so picking the same file again needs a fresh one"));
    }

    [Test]
    public async Task SavingARenameSendsTheNameWithTheExtensionAndReloadsTheList()
    {
        await using var ctx = FrontendContext.Create();

        var renamed = new List<(Guid FileId, string FileName)>();
        var loads = 0;
        var file = PdfFile();

        var component = new Card
        {
            Files = [file],
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([file]);
            },
            RenameFile = (item, name, _) =>
            {
                renamed.Add((item.FileId, name));

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await MenuItem(component, "Přejmenovat").ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nový název" });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty, "a saved rename leaves the input"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renamed, Is.EqualTo([(file.FileId, "Nový název.pdf")]));
            Assert.That(loads, Is.EqualTo(2), "the list reloads after a rename");
        }
    }

    [Test]
    public async Task TheRowStarSetsOrUnsetsTheMainFileAndTheReloadedListMovesIt()
    {
        await using var ctx = FrontendContext.Create();

        var main = File("rozhodnuti.pdf") with { IsMain = true };
        var other = File("priloha.pdf");
        IReadOnlyList<FileListItem> listed = [main, other];
        var changes = new List<(Guid FileId, bool Main)>();

        var component = new Card
        {
            LoadFiles = _ => Task.FromResult(listed),
            ChangeMainFile = (file, isMain, _) =>
            {
                changes.Add((file.FileId, isMain));
                listed = [main with { IsMain = false }, other with { IsMain = isMain }];

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        var stars = component.FindAll(".ec-file-row .ec-main-star");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stars.Select(static star => star.ClassList.Contains("ec-main-star-on")), Is.EqualTo([true, false]), "only the main file's star is filled");
            Assert.That(stars.Select(static star => star.GetAttribute("aria-label")), Is.EqualTo(["Zrušit hlavní soubor rozhodnuti.pdf", "Nastavit priloha.pdf jako hlavní soubor"]));
        }

        await stars[1].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(
            component.FindAll(".ec-file-row .ec-main-star").Select(static star => star.ClassList.Contains("ec-main-star-on")),
            Is.EqualTo([false, true]),
            "the reloaded list moves the mark"));

        await component.FindAll(".ec-file-row .ec-main-star")[1].ClickAsync(new MouseEventArgs());

        Assert.That(changes, Is.EqualTo([(other.FileId, true), (other.FileId, false)]), "an empty star sets the mark, a filled one unsets it");
    }

    [Test]
    public async Task AFailedChangeOfTheMainFileSaysSo()
    {
        await using var ctx = FrontendContext.Create();

        var component = new Card
        {
            Files = [PdfFile()],
            ChangeMainFile = static (_, _, _) => throw new HttpRequestException("offline"),
        }
            .Render(ctx);

        await component.Find(".ec-file-row .ec-main-star").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("Hlavní soubor se nepodařilo změnit. Zkuste to za chvíli znovu.")));
    }

    private static FileUploadResult Stored(FileListItem file, FileUploadOutcome outcome)
    {
        return new FileUploadResult { File = file with { Version = file.Version + 1, VersionCount = file.VersionCount + 1 }, Outcome = outcome };
    }

    private static FileListItem Versioned(string fileName, int version, int count)
    {
        return File(fileName) with { Version = version, VersionCount = count };
    }

    private static IReadOnlyList<FileVersionItem> VersionItems()
    {
        return
        [
            new FileVersionItem { Number = 3, SizeBytes = 3072, Created = new DateTime(2025, 9, 4, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jan.priklad@example.cz" },
            new FileVersionItem { Number = 2, SizeBytes = 2048, Created = new DateTime(2025, 9, 3, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jana.ukazkova@example.cz" },
            new FileVersionItem { Number = 1, SizeBytes = 1024, Created = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jan.priklad@example.cz" },
        ];
    }

    private static FileUploadResult Stored(string name)
    {
        return new FileUploadResult
        {
            File = new FileListItem { FileId = Guid.CreateVersion7(), FileName = name, SizeBytes = 1, Created = DateTime.UtcNow, Version = 1, VersionCount = 1 },
            Outcome = FileUploadOutcome.NewFile,
        };
    }

    [Test]
    public async Task ABatchReportsNewVersionsAndUnchangedFilesAndHighlightsWhatItStored()
    {
        await using var ctx = FrontendContext.Create();

        var stored = File("nový.txt");
        var same = Versioned("stejný.txt", 2, 2);
        var added = Versioned("další.txt", 3, 3);
        var component = new Card
        {
            Files = [stored, same, added],
            UploadFile = (picked, _) => Task.FromResult(picked.Name switch
            {
                "nový.txt" => new FileUploadResult { File = stored, Outcome = FileUploadOutcome.NewFile },
                "stejný.txt" => new FileUploadResult { File = same, Outcome = FileUploadOutcome.Unchanged },
                _ => new FileUploadResult { File = added, Outcome = FileUploadOutcome.NewVersion },
            }),
        }
            .Render(ctx);

        component.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "nový.txt"),
            InputFileContent.CreateFromText("b", "stejný.txt"),
            InputFileContent.CreateFromText("c", "další.txt"));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-notice span"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-file-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-file-notice span").Select(static notice => notice.TextContent),
                Is.EqualTo(["stejný.txt: obsah je stejný jako poslední verze, nová verze nevznikla.", "další.txt: nahráno jako verze 3."]));
            Assert.That(rows[0].ClassList, Does.Contain("ec-file-row-fresh"));
            Assert.That(rows[0].QuerySelector(".ec-badge-fresh")!.TextContent.Trim(), Is.EqualTo("nový"));
            Assert.That(rows[1].ClassList, Does.Not.Contain("ec-file-row-fresh"), "an unchanged file marks nothing");
            Assert.That(rows[1].QuerySelector(".ec-badge-fresh"), Is.Null);
            Assert.That(rows[2].ClassList, Does.Contain("ec-file-row-fresh"));
            Assert.That(rows[2].QuerySelector(".ec-badge-fresh")!.TextContent.Trim(), Is.EqualTo("nová verze"));
        }
    }

    [Test]
    public async Task ANewVersionWithAnotherExtensionIsRefusedWithoutAnUpload()
    {
        await using var ctx = FrontendContext.Create();

        var calls = 0;
        var component = new Card
        {
            Files = [File("a.pdf")],
            UploadFileVersion = (target, _, _) =>
            {
                calls++;

                return Task.FromResult(Stored(target, FileUploadOutcome.NewVersion));
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await MenuItem(component, "Nahrát novou verzi").ClickAsync(new MouseEventArgs());

        VersionInput(component).UploadFiles(InputFileContent.CreateFromText("x", "návrh.docx"));

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("návrh.docx: nová verze souboru a.pdf musí mít příponu .pdf.")));

        Assert.That(calls, Is.Zero);
    }

    [Test]
    public async Task AVersionPreviewedFromTheVersionsOpensThatVersion()
    {
        await using var ctx = FrontendContext.Create();

        var loaded = new List<int?>();
        var component = new Card
        {
            Files = [Versioned("a.txt", 3, 3)],
            LoadFileVersions = static (_, _) => Task.FromResult(VersionItems()),
            LoadContent = (_, version, _) =>
            {
                loaded.Add(version);

                return Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray()));
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await MenuItem(component, "Verze (3)").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-main")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1)));

        Assert.That(loaded, Is.EqualTo(new int?[] { 2 }));
    }

    [Test]
    public async Task SmazatInThePreviewKeepsThePreviewOpenUntilTheDeleteIsDone()
    {
        await using var ctx = FrontendContext.Create();

        IReadOnlyList<FileListItem> files = [Versioned("a.txt", 3, 3)];
        var component = new Card
        {
            LoadFiles = _ => Task.FromResult(files),
            LoadFileVersions = static (_, _) => Task.FromResult(VersionItems()),
            DeleteFile = (_, _) =>
            {
                files = [];

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await MenuItem(component, "Verze (3)").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-main")!.ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1)));

        await component.Find(".ec-preview-actions [aria-label='Další akce se souborem']").ClickAsync(new MouseEventArgs());
        await MenuItem(component, "Smazat").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-modal-large"), Has.Count.EqualTo(1), "the preview stays open under the confirmation");

        await component.Find("dialog[aria-label='Smazat soubor'] .ec-modal-footer button.ec-button-secondary").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("dialog[aria-label='Smazat soubor']"), Is.Empty);
            Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1), "a cancel leaves the preview on the version it showed");
        }

        await component.Find(".ec-preview-actions [aria-label='Další akce se souborem']").ClickAsync(new MouseEventArgs());
        await MenuItem(component, "Smazat").ClickAsync(new MouseEventArgs());
        await component.Find("dialog[aria-label='Smazat soubor'] .ec-modal-footer button.ec-button-danger").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-modal-large"), Is.Empty, "the preview closes once the file is deleted");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RestoringAnOlderVersionShowsTheCurrentOneAndMarksAStoredVersion(bool stored)
    {
        await using var ctx = FrontendContext.Create();

        IReadOnlyList<FileListItem> files = [Versioned("a.txt", 3, 3)];
        var restored = new List<int>();
        var component = new Card
        {
            LoadFiles = _ => Task.FromResult(files),
            LoadFileVersions = static (_, _) => Task.FromResult(VersionItems()),
            RestoreFileVersion = (file, number, _) =>
            {
                restored.Add(number);

                if (stored)
                    files = [file with { Version = 4, VersionCount = 4 }];

                return Task.FromResult(files[0]);
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await MenuItem(component, "Verze (3)").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-main")!.ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1)));

        await component.FindAll(".ec-preview-versions-old button").Single(static button => button.TextContent.Contains("Vrátit se k této verzi", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("dialog[aria-label='Vrátit se k verzi'] .ec-confirm-text").TextContent, Is.EqualTo("Vznikne nová verze souboru a.txt s obsahem verze 2."));

        await component.Find("dialog[aria-label='Vrátit se k verzi'] .ec-modal-footer button.ec-button-primary").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Is.Empty, "the preview shows the current version"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored, Is.EqualTo([2]));
            Assert.That(component.FindAll("dialog[aria-label='Vrátit se k verzi']"), Is.Empty);
            Assert.That(component.FindAll(".ec-modal-large"), Has.Count.EqualTo(1), "the preview stays open");
            Assert.That(component.FindAll(".ec-file-row .ec-badge-fresh").Select(static badge => badge.TextContent.Trim()), Is.EqualTo(stored ? ["nová verze"] : Array.Empty<string>()));
            Assert.That(component.FindAll(".ec-file-notice"), Is.Empty, "a restore shows no notice");
        }
    }

    [Test]
    public async Task ACancelAfterTheDeleteFoundTheFileGoneClosesItsPreview()
    {
        await using var ctx = FrontendContext.Create();

        IReadOnlyList<FileListItem> files = [File("a.txt")];
        var component = new Card
        {
            LoadFiles = _ => Task.FromResult(files),
            DeleteFile = (_, _) =>
            {
                files = [];

                throw new ApiException(HttpStatusCode.NotFound, responseBody: null);
            },
        }
            .Render(ctx);

        await component.Find(".ec-file-name").ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-modal-large"), Has.Count.EqualTo(1)));
        await component.Find(".ec-preview-actions [aria-label='Další akce se souborem']").ClickAsync(new MouseEventArgs());
        await MenuItem(component, "Smazat").ClickAsync(new MouseEventArgs());
        await component.Find("dialog[aria-label='Smazat soubor'] .ec-modal-footer button.ec-button-danger").ClickAsync(new MouseEventArgs());
        await component.Find("dialog[aria-label='Smazat soubor'] .ec-modal-footer button.ec-button-secondary").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-modal-large"), Is.Empty, "the preview of a file found gone closes with the confirmation");
    }

    [Test]
    public async Task TheFileMenuOpensWithTheDownloadThatTheRowCarriesOnlyAboveLg()
    {
        await using var ctx = FrontendContext.Create();

        var file = PdfFile();
        var component = new Card { Files = [file] }.Render(ctx);

        await component.Find(".ec-file-menu-narrow button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());

        var first = component.FindAll("[role=menuitem]")[0];

        Assert.That(first.TextContent.Trim(), Is.EqualTo("Stáhnout"), "below lg the menu is the row's only way to download");

        await first.ClickAsync(new MouseEventArgs());

        var browserFiles = (StubBrowserFiles)ctx.Services.GetRequiredService<IBrowserFiles>();

        Assert.That(browserFiles.Downloaded, Is.EqualTo([(file.FileId, (int?)null, file.FileName)]));
    }

    private static AngleSharp.Dom.IElement MenuItem(IRenderedComponent<FilesCard> component, string text)
    {
        return component.FindAll("[role=menuitem]").Single(item => string.Equals(item.TextContent.Trim(), text, StringComparison.Ordinal));
    }

    private static IRenderedComponent<InputFile> VersionInput(IRenderedComponent<FilesCard> component)
    {
        return component.FindComponents<InputFile>().Single(static input => input.Instance.AdditionalAttributes is { } attributes && attributes.TryGetValue("id", out var id) && Equals(id, "files-version-input"));
    }

    private static async Task OpenMenu(IRenderedComponent<FilesCard> component)
    {
        await component.Find(".ec-file-menu-wide button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
    }

    private static InputFileChangeEventArgs Change(params string[] fileNames)
    {
        return new InputFileChangeEventArgs([.. fileNames.Select(static name => new NamedFile(name))]);
    }

    /// <summary>
    /// Drops onto the first input, the fresh one the drop zone hands a drop to.
    /// </summary>
    private static async Task Drop(IRenderedComponent<FilesCard> component, params string[] fileNames)
    {
        await component.InvokeAsync(async () => await component.FindComponents<InputFile>()[0].Instance.OnChange.InvokeAsync(Change(fileNames)));
    }

    private static FileListItem PdfFile()
    {
        return File("Rozhodnutí o přestupku.pdf");
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            SizeBytes = 1024,
            Created = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc),
            Version = 1,
            VersionCount = 1,
        };
    }

    private sealed class NamedFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size => 1;

        public string ContentType => "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return new MemoryStream("a"u8.ToArray());
        }
    }

    private sealed class Card
    {
        public string Title { get; init; } = "Soubory spisu";

        public string UploadTarget { get; init; } = "ke spisu";

        public bool Compact { get; init; }

        public string ArchiveUrl { get; init; } = "/api/cases/1/files/archive";

        public IReadOnlyList<FileListItem> Files { get; init; } = [];

        public Func<CancellationToken, Task<IReadOnlyList<FileListItem>>>? LoadFiles { get; init; }

        public Func<IBrowserFile, CancellationToken, Task<FileUploadResult>> UploadFile { get; init; } = static (file, _) => Task.FromResult(Stored(file.Name));

        public Func<FileListItem, string, CancellationToken, Task> RenameFile { get; init; } = static (_, _, _) => Task.CompletedTask;

        public Func<FileListItem, IBrowserFile, CancellationToken, Task<FileUploadResult>> UploadFileVersion { get; init; } = static (file, _, _) => Task.FromResult(Stored(file, FileUploadOutcome.NewVersion));

        public Func<FileListItem, CancellationToken, Task<IReadOnlyList<FileVersionItem>>> LoadFileVersions { get; init; } = static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]);

        public Func<FileListItem, int, CancellationToken, Task> DeleteFileVersion { get; init; } = static (_, _, _) => Task.CompletedTask;

        public Func<FileListItem, int, CancellationToken, Task<FileListItem>> RestoreFileVersion { get; init; } = static (file, _, _) => Task.FromResult(file);

        public Func<FileListItem, int?, CancellationToken, Task<Stream>> LoadContent { get; init; } = static (_, _, _) => Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray()));

        public Func<FileListItem, bool, CancellationToken, Task> ChangeMainFile { get; init; } = static (_, _, _) => Task.CompletedTask;

        public Func<FileListItem, CancellationToken, Task> DeleteFile { get; init; } = static (_, _) => Task.CompletedTask;

        public IRenderedComponent<FilesCard> Render(BunitContext ctx)
        {
            var files = this.Files;
            var loadFiles = this.LoadFiles ?? (_ => Task.FromResult(files));

            return ctx.Render<FilesCard>(parameters => parameters
                .Add(static card => card.Title, this.Title)
                .Add(static card => card.State, new FilesCardState(loadFiles))
                .Add(static card => card.UploadFile, this.UploadFile)
                .Add(static card => card.LoadContent, this.LoadContent)
                .Add(static card => card.UploadFileVersion, this.UploadFileVersion)
                .Add(static card => card.LoadFileVersions, this.LoadFileVersions)
                .Add(static card => card.DeleteFileVersion, this.DeleteFileVersion)
                .Add(static card => card.RestoreFileVersion, this.RestoreFileVersion)
                .Add(static card => card.RenameFile, this.RenameFile)
                .Add(static card => card.DeleteFile, this.DeleteFile)
                .Add(static card => card.ChangeMainFile, this.ChangeMainFile)
                .Add(static card => card.OwnerGoneError, "spis už neexistuje.")
                .Add(static card => card.UploadTarget, this.UploadTarget)
                .Add(static card => card.ArchiveUrl, this.ArchiveUrl)
                .Add(static card => card.Compact, this.Compact));
        }
    }
}
