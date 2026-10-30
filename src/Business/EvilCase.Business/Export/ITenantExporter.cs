namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// Everything the caller's tenant holds as one zip archive (SDD-022).
/// </summary>
public interface ITenantExporter
{
    /// <summary>
    /// Names the export by the Prague date of now.
    /// </summary>
    public TenantExport CreateTenantExport();

    /// <summary>
    /// Streams the archive to the target from one database snapshot, writing only asynchronously. A failure or a
    /// cancellation throws, and what was written then lacks the archive's end.
    /// </summary>
    public Task WriteTenantExport(TenantExport export, Stream target, CancellationToken token);
}
