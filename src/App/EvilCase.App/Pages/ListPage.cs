using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace EvilBrains.EvilCase.App.Pages;

/// <summary>
/// A page whose list starts afresh on every navigation to the page, so its toolbar always shows
/// what the address says, also for an address the page already sits on.
/// </summary>
public abstract class ListPage : ComponentBase, IDisposable
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>
    /// Keys the list; it changes with every navigation.
    /// </summary>
    protected int ListKey { get; private set; }

    public void Dispose()
    {
        this.Navigation.LocationChanged -= this.OnLocationChanged;
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        this.Navigation.LocationChanged += this.OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        this.ListKey++;

        _ = this.InvokeAsync(this.StateHasChanged);
    }
}
