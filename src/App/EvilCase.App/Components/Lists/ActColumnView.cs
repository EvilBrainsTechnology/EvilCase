using EvilBrains.EvilCase.Api.Contract.Acts;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

public sealed record ActColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public ActSortKey? Sort { get; init; }

    public required RenderFragment<ActListItem> Cell { get; init; }

    // A column the card variant leaves out carries no Line.
    public RenderFragment<ActListItem>? Line { get; init; }
}
