namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// export.json, the archive's root document.
/// </summary>
internal sealed record ExportManifest
{
    public string Type { get; } = "export";

    public required int FormatVersion { get; init; }

    public required DateTime Exported { get; init; }

    public required string ApplicationVersion { get; init; }

    public required Guid TenantId { get; init; }

    public required string TenantName { get; init; }
}
