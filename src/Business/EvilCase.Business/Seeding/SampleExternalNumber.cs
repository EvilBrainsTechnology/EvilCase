namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed record SampleExternalNumber
{
    public required string Value { get; init; }

    public required string AssignedByKey { get; init; }
}
