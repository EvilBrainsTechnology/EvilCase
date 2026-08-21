namespace EvilBrains.EvilCase.Files;

internal static class FileBlobPath
{
    /// <summary>
    /// Two levels of fan-out so one directory never holds every blob of a tenant. The characters come from
    /// the end of the asset's UUIDv7: its front is a timestamp and would put a day's uploads together.
    /// The extension, where the uploaded name carries a safe one, rides along on the blob's own name.
    /// </summary>
    public static string For(in Guid tenantId, in Guid fileAssetId, string? fileName = null)
    {
        var tenant = tenantId.ToString("D", CultureInfo.InvariantCulture);
        var id = fileAssetId.ToString("D", CultureInfo.InvariantCulture);
        var hex = fileAssetId.ToString("N", CultureInfo.InvariantCulture);

        return $"{tenant}/{hex[^2..]}/{hex[^4..^2]}/{id}{BlobExtension.From(fileName)}";
    }
}
