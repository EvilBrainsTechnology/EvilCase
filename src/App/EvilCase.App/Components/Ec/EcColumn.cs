using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Ec;

public sealed record EcColumn<TItem>
{
    public required string Header { get; init; }

    /// <summary>
    /// The column's track in the row's grid, e.g. "var(--ec-list-col-date)" or "minmax(0, 1fr)".
    /// </summary>
    public required string Width { get; init; }

    /// <summary>
    /// The name of the host's sort-key enum member; a column without one does not sort.
    /// </summary>
    public string? SortKey { get; init; }

    public string? CellClass { get; init; }

    /// <summary>
    /// The cell that takes its own line where the row reflows (SDD-020).
    /// </summary>
    public bool Primary { get; init; }

    public required RenderFragment<TItem> Cell { get; init; }
}
