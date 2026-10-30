namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedContactReference
{
    public required Guid ContactId { get; init; }

    public required string Name { get; init; }
}
