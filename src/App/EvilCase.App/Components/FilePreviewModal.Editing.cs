using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Components;

public partial class FilePreviewModal
{
    // The link window is open while set.
    private string? linkHref;

    // The text in the editor as read; null while the preview only shows.
    private EditableText? editing;

    // Whether the editor's text differs from the one read.
    private bool dirty;

    private bool saving;

    private string? saveFailure;

    private TextEditor? editor;

    // Whether the read-only view showed the code formatted when the edit began.
    private bool editsFormattedCode;

    // Whether the editor shows its Změny, and the version they compare with; null compares with the version edited.
    private bool changesShown;

    private int? changesNumber;

    // The texts of the versions read for the Změny, by file, number and upload; null for a version not in UTF-8.
    private readonly Dictionary<(Guid FileId, int Number, DateTime Created), string?> changesTexts = [];

    // The last read of a version for the Změny failed.
    private bool changesFailed;

    private CancellationTokenSource? changesToken;

    // What a confirmed discard of unsaved changes goes on to do; set while the question shows.
    private Func<Task>? afterDiscard;

    // The editor is to open once the first file loads; the read under way carries it until it ends.
    private bool editOnLoad;

    private bool editOnShown;

    // Only the current version of a stored text or code file, read whole and valid UTF-8, is edited.
    private bool CanEdit => this.contentPreview is { Editable: not null, Ready: true, SignedContentName: null } && this.trail.Count == 0 && this.OlderVersion is null && !this.uploadingVersion;

    // Only the editor guards the close.
    private EventCallback CloseRequest => this.editing is null ? default : EventCallback.Factory.Create(this, this.RequestClose);

    private async Task StartEdit()
    {
        this.renaming = false;
        this.versionsOpen = false;
        this.versionFailure = null;
        this.saveFailure = null;
        this.dirty = false;
        this.editsFormattedCode = this.contentPreview?.ShowsFormattedCode == true;
        this.editing = this.contentPreview?.Editable;
        this.changesShown = false;
        this.changesNumber = null;
        this.changesFailed = false;

        try
        {
            await this.History.TrackSteps();
        }
        catch (JSException exception)
        {
            this.Logger.LogInformation(exception, "The browser's history steps are not tracked");
        }
    }

    private void LeaveEdit()
    {
        this.changesToken?.Cancel();
        this.changesTexts.Clear();
        this.editing = null;
        this.dirty = false;
        this.saveFailure = null;
    }

    private async Task ShowChanges(bool shown)
    {
        this.changesShown = shown;

        if (shown)
            await this.ReadChangesVersion();
        else if (this.changesToken is not null)
            await this.changesToken.CancelAsync();
    }

    private async Task PickChangesVersion(int number)
    {
        this.changesNumber = number == this.loadedNumber ? null : number;
        this.changesFailed = false;

        await this.ReadChangesVersion();
    }

    // Reads the version the Změny compare with, unless it is the one edited, read already or over the limit.
    private async Task ReadChangesVersion()
    {
        if (this.Current is not { } file
            || this.changesNumber is not { } number
            || this.versions?.FirstOrDefault(version => version.Number == number) is not { } version
            || version.SizeBytes > MaxTextBytes
            || this.changesTexts.ContainsKey((file.FileId, number, version.Created)))
        {
            return;
        }

        var previous = this.changesToken;
        var token = (this.changesToken = new CancellationTokenSource()).Token;

        this.changesFailed = false;

        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        try
        {
            await using var content = await this.LoadContent(file, number == file.Version ? null : number, token);
            var bytes = await FileContentPreview.ReadBytes(content, token);

            if (!token.IsCancellationRequested)
                this.changesTexts[(file.FileId, number, version.Created)] = EditableText.ParseOrDefault(bytes)?.Text;
        }
        catch (OperationCanceledException)
        {
            // Another version or the end of the edit took over.
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Loading version {Version} of file {FileId} for the changes failed", number, file.FileId);

            if (!token.IsCancellationRequested)
                this.changesFailed = true;
        }
    }

    private void EditorDirtyChanged(bool changed)
    {
        this.dirty = changed;
    }

    // A version that came while the editor was open shows once it closes.
    private Task ShowCurrent()
    {
        if (this.editing is null && this.Current is { } file && file.FileId == this.loadedFileId && this.OlderVersion is null && file.Version != this.loadedNumber)
            this.Reload(file);

        return Task.CompletedTask;
    }

    private async Task EditorUnavailable()
    {
        this.LeaveEdit();
        this.versionFailure = "Úpravy se nepodařilo spustit. Zkuste to za chvíli znovu.";

        await this.ShowCurrent();
    }

    private async Task ApplyLink(string href)
    {
        this.linkHref = null;

        await this.contentPreview!.SetLink(href);
    }

    private async Task CancelEdit()
    {
        if (this.saving)
            return;

        if (this.dirty)
        {
            this.afterDiscard = this.ShowCurrent;
        }
        else
        {
            this.LeaveEdit();

            await this.ShowCurrent();
        }
    }

    // The X, a click outside or Escape while editing: unsaved changes are confirmed before the preview closes. A save
    // under way finishes first.
    private async Task RequestClose()
    {
        if (this.saving)
            return;

        if (this.dirty)
            this.afterDiscard = async () => await this.OpenChanged.InvokeAsync(false);
        else
            await this.OpenChanged.InvokeAsync(false);
    }

    // Covers links and the browser's Back and Forward; leaving the site is asked by the browser through
    // ConfirmExternalNavigation. A Back or Forward goes on after the discard as the same step through the history.
    private async Task ConfirmNavigation(LocationChangingContext context)
    {
        var held = this.dirty;

        if (held)
            context.PreventNavigation();

        var target = context.TargetLocation;
        var distance = 0;

        if (!context.IsNavigationIntercepted)
        {
            try
            {
                distance = await this.History.TakeStep(target);
            }
            catch (JSException exception)
            {
                this.Logger.LogInformation(exception, "The browser's history step is not known");
            }
        }

        if (!held)
            return;

        this.afterDiscard = async () =>
        {
            if (distance == 0)
                this.Navigation.NavigateTo(target);
            else
                await this.History.Go(distance);
        };
    }

    private async Task Discard()
    {
        var next = this.afterDiscard!;

        this.afterDiscard = null;
        this.LeaveEdit();

        await next();
    }

    private void KeepEditing(bool open)
    {
        if (!open)
            this.afterDiscard = null;
    }

    // The text goes up as a new version by the same call as Nová verze; a success shows the current version.
    private async Task Save()
    {
        if (this.editing is not { } edited || !this.dirty || this.saving || this.Current is not { } file)
            return;

        this.saving = true;
        this.saveFailure = null;

        string? failure;

        try
        {
            var text = FileTypeDisplay.Preview(file.FileName) == FilePreviewKind.Markdown
                ? await this.contentPreview!.ReadMarkdown()
                : await this.editor!.ReadText();
            var content = edited.Encode(text);

            failure = (await this.UploadVersion(file, new EditedFile(file.FileName, content))).Failure;
        }
        catch (JSException exception)
        {
            this.Logger.LogWarning(exception, "Reading the edited text of file {FileAssetId} failed", file.FileId);

            failure = "Text se nepodařilo přečíst z editoru. Zkuste to znovu.";
        }
        finally
        {
            this.saving = false;
        }

        if (failure is not null)
        {
            this.saveFailure = failure;

            return;
        }

        this.LeaveEdit();

        await this.ShowCurrent();
        await this.VersionChanged.InvokeAsync(null);
    }
}
