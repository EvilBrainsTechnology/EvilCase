namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// labels.json in labels.
/// </summary>
internal sealed record ExportedLabelList
{
    public string Type { get; } = "labels";

    public required IReadOnlyList<ExportedLabel> Labels { get; init; }
}
