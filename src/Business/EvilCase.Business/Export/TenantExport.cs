namespace EvilBrains.EvilCase.Business.Export;

public sealed record TenantExport
{
    public required string FileName { get; init; }

    /// <summary>
    /// The export time export.json carries.
    /// </summary>
    public required DateTimeOffset Started { get; init; }
}
