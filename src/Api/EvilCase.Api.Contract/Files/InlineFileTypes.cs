namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The file types a browser shows natively: raster images, audio and video. Their content is served
/// inline under the type detected here, never under the stored one.
/// </summary>
public static class InlineFileTypes
{
    private static readonly Dictionary<string, string> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["png"] = "image/png",
        ["gif"] = "image/gif",
        ["webp"] = "image/webp",
        ["avif"] = "image/avif",
        ["bmp"] = "image/bmp",
        ["mp3"] = "audio/mpeg",
        ["m4a"] = "audio/mp4",
        ["wav"] = "audio/wav",
        ["ogg"] = "audio/ogg",
        ["oga"] = "audio/ogg",
        ["mp4"] = "video/mp4",
        ["webm"] = "video/webm",
        ["m4v"] = "video/mp4",
        ["mov"] = "video/quicktime",
    };

    /// <summary>
    /// The media type of a file shown inline, or <see langword="null"/> for any other file.
    /// </summary>
    public static string? MediaType(string fileName)
    {
        return MediaTypes.GetValueOrDefault(Path.GetExtension(fileName).TrimStart('.'));
    }
}
