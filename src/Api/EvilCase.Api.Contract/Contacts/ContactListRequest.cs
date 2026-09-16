using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest : ListRequest<ContactSortKey>
{
    public override ContactSortKey Sort { get; init; } = ContactSortKey.Name;

    public string? Search { get; init; }

    public ContactKind? Kind { get; init; }
}
