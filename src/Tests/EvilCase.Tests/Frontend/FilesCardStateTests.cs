using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilesCardStateTests
{
    [Test]
    public async Task AListWithAFileGoesWideAndAnEmptyOneCompact()
    {
        IReadOnlyList<FileListItem> files = [File()];
        var placed = 0;
        var state = new FilesCardState(_ => Task.FromResult(files));

        state.Placed += (_, _) => placed++;

        await state.EnsureLoaded(NullLogger.Instance);

        Assert.That(state.Wide, Is.True);

        files = [];
        await state.Reload(NullLogger.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Wide, Is.False);
            Assert.That(placed, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task TheCardKeepsItsPlaceWhileAnUploadRunsAndMovesWhenItEnds()
    {
        IReadOnlyList<FileListItem> files = [];
        var placed = 0;
        var state = new FilesCardState(_ => Task.FromResult(files));

        state.Placed += (_, _) => placed++;

        await state.EnsureLoaded(NullLogger.Instance);

        state.UploadStarted();
        files = [File()];
        await state.Reload(NullLogger.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Wide, Is.False, "a move recreates the card and the inputs an upload reads from");
            Assert.That(placed, Is.Zero);
        }

        state.UploadEnded();
        state.Place();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Wide, Is.True);
            Assert.That(placed, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task TheCardMovesOnlyWhenEveryUploadEnds()
    {
        IReadOnlyList<FileListItem> files = [];
        var state = new FilesCardState(_ => Task.FromResult(files));

        await state.EnsureLoaded(NullLogger.Instance);

        state.UploadStarted();
        state.UploadStarted();
        files = [File()];
        state.UploadEnded();
        await state.Reload(NullLogger.Instance);

        Assert.That(state.Wide, Is.False);

        state.UploadEnded();
        await state.Reload(NullLogger.Instance);

        Assert.That(state.Wide, Is.True);
    }

    [Test]
    public async Task TheFirstLoadRunsOnceForEveryCaller()
    {
        var loads = 0;
        var state = new FilesCardState(_ =>
        {
            loads++;

            return Task.FromResult<IReadOnlyList<FileListItem>>([]);
        });

        await Task.WhenAll(state.EnsureLoaded(NullLogger.Instance), state.EnsureLoaded(NullLogger.Instance));
        await state.EnsureLoaded(NullLogger.Instance);

        Assert.That(loads, Is.EqualTo(1));
    }

    [Test]
    public async Task AFailedLoadStaysCompactAndSaysSo()
    {
        var state = new FilesCardState(static _ => throw new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        await state.EnsureLoaded(NullLogger.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Loaded, Is.True);
            Assert.That(state.Wide, Is.False);
            Assert.That(state.ListFailure, Is.EqualTo("Soubory se nepodařilo načíst. Zkuste to za chvíli znovu."));
        }
    }

    private static FileListItem File()
    {
        return new FileListItem { FileId = Guid.CreateVersion7(), FileName = "a.pdf", SizeBytes = 1, Created = DateTime.UtcNow, Version = 1, VersionCount = 1 };
    }
}
