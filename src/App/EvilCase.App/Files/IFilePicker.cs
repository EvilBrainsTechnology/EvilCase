using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Files;

internal interface IFilePicker
{
    /// <summary>
    /// Opens the browser's file picker of a hidden input from a control that cannot be its label.
    /// </summary>
    public Task PickFile(ElementReference input, CancellationToken token);
}
