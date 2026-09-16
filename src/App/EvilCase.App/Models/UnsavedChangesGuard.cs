using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// Confirms leaving a dirty edit form before a link elsewhere on the page navigates away from it.
/// </summary>
internal static class UnsavedChangesGuard
{
    public static ValueTask<bool> ConfirmLeave(IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<bool>("confirm", "Neuložené změny se ztratí. Pokračovat?");
    }
}
