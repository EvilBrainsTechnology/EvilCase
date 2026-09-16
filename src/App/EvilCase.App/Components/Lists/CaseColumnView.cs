using EvilBrains.EvilCase.Api.Contract.Cases;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

/// <summary>
/// One column of the case list: its header, its table cell and the line it writes on the
/// card variant. A column the card leaves out carries no <see cref="Line"/>.
/// </summary>
public sealed record CaseColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public CaseSortKey? Sort { get; init; }

    public required RenderFragment<CaseListItem> Cell { get; init; }

    public RenderFragment<CaseListItem>? Line { get; init; }
}
