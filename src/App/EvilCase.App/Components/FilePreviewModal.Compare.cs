using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components;

public partial class FilePreviewModal
{
    // The file and the version the compare opened for, and whether it opened against the current version; null while
    // the preview does not compare.
    private (Guid FileId, int Number, bool WithCurrent)? compareStart;

    // The versions chosen in the compare; until then the pair it opened with.
    private (int Older, int Newer)? comparePair;

    // Until a pick, the version opened for against the current one when asked, else against the one before it; the
    // oldest against the current one. The current one is read each time, so a new version takes its place.
    private (int Older, int Newer)? ComparedPair
    {
        get
        {
            if (this.comparePair is { } picked)
                return picked;

            if (this.compareStart is not { } start || this.versions is not { } list || this.Current is not { } file)
                return null;

            if ((!start.WithCurrent || start.Number == file.Version) && list.FirstOrDefault(version => version.Number < start.Number) is { } previous)
                return (previous.Number, start.Number);

            return (start.Number, file.Version);
        }
    }

    private List<EcMenuItem> CompareActions(FileListItem file)
    {
        var actions = new List<EcMenuItem>
        {
            new() { Text = "Zavřít porovnání", Icon = AppIcons.Compare, OnSelect = EventCallback.Factory.Create(this, this.CloseCompare) },
            new() { Text = string.Create(CultureInfo.CurrentCulture, $"Stáhnout verzi {this.ComparedPair?.Newer ?? file.Version}"), Icon = AppIcons.Download, OnSelect = EventCallback.Factory.Create(this, async () => await this.DownloadCompared(file)) },
        };

        if (this.Files.Count > 1)
        {
            actions.Add(new EcMenuItem { Text = "Předchozí soubor", Icon = AppIcons.ChevronLeft, OnSelect = EventCallback.Factory.Create(this, async () => await this.Step(-1)), DividerBefore = true });
            actions.Add(new EcMenuItem { Text = "Další soubor", Icon = AppIcons.ChevronRight, OnSelect = EventCallback.Factory.Create(this, async () => await this.Step(1)) });
        }

        return actions;
    }

    // An older version compares with the current one, the current with the one before it.
    private void StartCompare(FileListItem file)
    {
        this.renaming = false;
        this.versionsOpen = false;
        this.versionFailure = null;
        this.compareStart = (file.FileId, this.OlderVersion ?? file.Version, WithCurrent: this.OlderVersion is not null);
        this.comparePair = null;
    }

    private void CloseCompare()
    {
        this.compareStart = null;
        this.comparePair = null;
    }

    // The newer version of the compare is saved under the file's name.
    private async Task DownloadCompared(FileListItem file)
    {
        await this.DownloadVersion(file, this.ComparedPair?.Newer ?? file.Version);
    }
}
