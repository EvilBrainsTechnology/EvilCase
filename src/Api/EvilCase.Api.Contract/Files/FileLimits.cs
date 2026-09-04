namespace EvilBrains.EvilCase.Api.Contract.Files;

public static class FileLimits
{
    public const long MaxUploadBytes = 100L * 1024 * 1024;

    /// <summary>
    /// The whole multipart request, the envelope around the file included. Kestrel's default caps a
    /// request at 30 MB, so the upload names its own.
    /// </summary>
    public const long MaxUploadRequestBytes = MaxUploadBytes + (1024 * 1024);

    /// <summary>
    /// Mirrors <c>FileAsset.FileName</c>'s <c>MaxLength</c>.
    /// </summary>
    public const int MaxFileNameLength = 256;

    /// <summary>
    /// Mirrors <c>FileAsset.MediaType</c>'s <c>MaxLength</c>.
    /// </summary>
    public const int MaxMediaTypeLength = 128;
}
