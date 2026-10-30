namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// !stitky.json in Štítky.
/// </summary>
internal sealed record ExportedLabelList
{
    public string Type { get; } = "labels";

    public required IReadOnlyList<ExportedLabel> Labels { get; init; }
}
