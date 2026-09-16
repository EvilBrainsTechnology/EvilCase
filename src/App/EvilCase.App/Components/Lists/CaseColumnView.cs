using EvilBrains.EvilCase.Api.Contract.Cases;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

/// <summary>
/// A column the card variant leaves out carries no <see cref="Line"/>.
/// </summary>
public sealed record CaseColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public CaseSortKey? Sort { get; init; }

    public required RenderFragment<CaseListItem> Cell { get; init; }

    public RenderFragment<CaseListItem>? Line { get; init; }
}
