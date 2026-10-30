using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
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
        await using var ctx = NewContext();

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
        await using var ctx = NewContext();

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
        using var ctx = NewContext();

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
        await using var ctx = NewContext();

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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nový název" });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty, "a saved rename leaves the input"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renamed, Is.EqualTo([(file.FileId, "Nový název.pdf")]));
            Assert.That(loads, Is.EqualTo(2), "the list reloads after a rename");
        }
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

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();

        // FileDropZone imports its own module from a path carrying a version query string, which
        // no SetupModule can name, and reads a property bUnit's runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var downloader = new StubFileDownloader();
        ctx.Services.AddSingleton(downloader);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubFilePicker());
        ctx.Services.AddSingleton<IFilePicker>(static sp => sp.GetRequiredService<StubFilePicker>());
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());
        ctx.Services.AddSingleton<ITextEditors>(new StubTextEditors());
        ctx.Services.AddSingleton<IBrowserHistory>(new StubBrowserHistory());
        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession());

        return ctx;
    }

    [Test]
    public async Task ABatchReportsNewVersionsAndUnchangedFilesAndHighlightsWhatItStored()
    {
        await using var ctx = NewContext();

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
        await using var ctx = NewContext();

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
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        VersionInput(component).UploadFiles(InputFileContent.CreateFromText("x", "návrh.docx"));

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("návrh.docx: nová verze souboru a.pdf musí mít příponu .pdf.")));

        Assert.That(calls, Is.Zero);
    }

    [Test]
    public async Task AVersionPreviewedFromTheVersionsOpensThatVersion()
    {
        await using var ctx = NewContext();

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
        await component.FindAll("[role=menuitem]").Single(static item => string.Equals(item.TextContent.Trim(), "Verze (3)", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-main")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1)));

        Assert.That(loaded, Is.EqualTo(new int?[] { 2 }));
    }

    private static IRenderedComponent<InputFile> VersionInput(IRenderedComponent<FilesCard> component)
    {
        return component.FindComponents<InputFile>().Single(static input => input.Instance.AdditionalAttributes is { } attributes && attributes.TryGetValue("id", out var id) && Equals(id, "files-version-input"));
    }

    private static async Task OpenMenu(IRenderedComponent<FilesCard> component)
    {
        await component.Find(".ec-file-actions button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
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

        public Func<FileListItem, int?, CancellationToken, Task<Stream>> LoadContent { get; init; } = static (_, _, _) => Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray()));

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
                .Add(static card => card.RenameFile, this.RenameFile)
                .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
                .Add(static card => card.OwnerGoneError, "spis už neexistuje.")
                .Add(static card => card.UploadTarget, this.UploadTarget)
                .Add(static card => card.ArchiveUrl, this.ArchiveUrl)
                .Add(static card => card.Compact, this.Compact));
        }
    }
}
