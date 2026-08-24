namespace EvilBrains.EvilCase.Files;

internal static class FileBlobPath
{
    /// <summary>
    /// Two levels of fan-out so one directory never holds every blob of a tenant. The characters come from
    /// the end of the asset's UUIDv7: its front is a timestamp and would put a day's uploads together.
    /// </summary>
    public static string For(in Guid tenantId, in Guid fileAssetId)
    {
        var tenant = tenantId.ToString("D", CultureInfo.InvariantCulture);
        var fileAsset = fileAssetId.ToString("D", CultureInfo.InvariantCulture);
        var hex = fileAssetId.ToString("N", CultureInfo.InvariantCulture);

        return $"{tenant}/{hex[^2..]}/{hex[^4..^2]}/{fileAsset}";
    }
}
