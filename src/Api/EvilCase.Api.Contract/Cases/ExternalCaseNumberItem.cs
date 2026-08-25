namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record ExternalCaseNumberItem
{
    public required Guid Id { get; init; }

    public required string Value { get; init; }

    public required Guid AssignedByContactId { get; init; }

    public required string AssignedByContactName { get; init; }
}
