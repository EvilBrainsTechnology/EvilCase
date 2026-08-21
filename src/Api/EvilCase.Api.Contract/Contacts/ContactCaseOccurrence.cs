namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactCaseOccurrence
{
    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string CaseTitle { get; init; }

    public required DateOnly CaseDate { get; init; }

    public required string ExternalNumber { get; init; }
}
