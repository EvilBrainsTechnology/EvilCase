using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest : ListRequest
{
    // A contact list reads by name, from A to Z (SDD-011).
    public ContactListRequest()
    {
        this.SortDirection = ListSortDirection.Ascending;
    }

    public string? Search { get; init; }

    public ContactKind? Kind { get; init; }

    public ContactSortKey Sort { get; init; } = ContactSortKey.Name;
}
