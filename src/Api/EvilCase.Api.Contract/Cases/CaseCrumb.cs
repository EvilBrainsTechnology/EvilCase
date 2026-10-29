namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseCrumb
{
    public required Guid CaseId { get; init; }

    public required string Title { get; init; }
}
