using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest : ListRequest
{
    public ContactKind? Kind { get; init; }

    public ContactSortKey Sort { get; init; } = ContactSortKey.Name;
}
