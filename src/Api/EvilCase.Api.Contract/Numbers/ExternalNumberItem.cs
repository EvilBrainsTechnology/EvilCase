namespace EvilBrains.EvilCase.Api.Contract.Numbers;

public sealed record ExternalNumberItem
{
    public required Guid NumberId { get; init; }

    public required string Value { get; init; }

    public required Guid AssignedByContactId { get; init; }

    public required string AssignedByContactName { get; init; }
}
