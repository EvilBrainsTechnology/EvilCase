using EvilBrains.EvilCase.Api.Contract.Contacts;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

public sealed record ContactColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public ContactSortKey? Sort { get; init; }

    public required RenderFragment<ContactListItem> Cell { get; init; }

    // A column the card variant leaves out carries no Line.
    public RenderFragment<ContactListItem>? Line { get; init; }
}
