using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest : ListRequest<ContactSortKey>
{
    // A contact list reads by name, from A to Z (SDD-011).
    public override ContactSortKey Sort { get; init; } = ContactSortKey.Name;

    public string? Search { get; init; }

    public ContactKind? Kind { get; init; }
}
