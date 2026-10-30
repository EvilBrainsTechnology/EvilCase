namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedLabelReference
{
    public required Guid LabelId { get; init; }

    public required string Name { get; init; }
}
