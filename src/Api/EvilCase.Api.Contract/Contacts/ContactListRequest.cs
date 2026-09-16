using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest : SortableListRequest<ContactSortKey>
{
    public override ContactSortKey Sort { get; init; } = ContactSortKey.Name;

    public string? Search { get; init; }

    public ContactKind? Kind { get; init; }
}
