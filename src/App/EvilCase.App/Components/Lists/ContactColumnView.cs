using EvilBrains.EvilCase.Api.Contract.Contacts;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

/// <summary>
/// One column of the contact list: its header, its table cell and the line it writes on the
/// card variant. A column the card leaves out carries no <see cref="Line"/>.
/// </summary>
public sealed record ContactColumnView
{
    public required string Header { get; init; }

    public string? CellClass { get; init; }

    public ContactSortKey? Sort { get; init; }

    public required RenderFragment<ContactListItem> Cell { get; init; }

    public RenderFragment<ContactListItem>? Line { get; init; }
}
