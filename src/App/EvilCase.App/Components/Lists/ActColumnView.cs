using EvilBrains.EvilCase.Api.Contract.Acts;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

/// <summary>
/// One column of the act list: its header, its table cell and the line it writes on the
/// card variant. A column the card leaves out carries no <see cref="Line"/>.
/// </summary>
public sealed record ActColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public ActSortKey? Sort { get; init; }

    public required RenderFragment<ActListItem> Cell { get; init; }

    public RenderFragment<ActListItem>? Line { get; init; }
}
