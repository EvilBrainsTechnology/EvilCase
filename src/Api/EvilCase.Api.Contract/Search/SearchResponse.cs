using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchResponse
{
    public required SearchGroup<CaseListItem> Cases { get; init; }

    public required SearchGroup<ActListItem> Acts { get; init; }

    public required SearchGroup<ContactListItem> Contacts { get; init; }

    public required SearchGroup<FileSearchItem> Files { get; init; }

    public required SearchGroup<CommentSearchItem> Comments { get; init; }
}
