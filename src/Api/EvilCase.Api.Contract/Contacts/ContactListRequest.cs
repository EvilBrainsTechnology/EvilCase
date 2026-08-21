namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListRequest
{
    /// <summary>
    /// Matched against the name and the data box id.
    /// </summary>
    public string? Search { get; init; }
}
