namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest
{
    public string? Search { get; init; }
}
