namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListResponse
{
    public required IReadOnlyList<ContactListItem> Items { get; init; }

    public required int TotalCount { get; init; }
}
