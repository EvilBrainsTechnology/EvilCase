namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListResponse
{
    public required IReadOnlyList<ContactListItem> Items { get; init; }

    /// <summary>
    /// Every row the filter leaves, whatever the page asks for.
    /// </summary>
    public required int TotalCount { get; init; }
}
