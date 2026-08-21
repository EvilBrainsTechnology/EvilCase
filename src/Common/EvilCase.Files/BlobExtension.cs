namespace EvilBrains.EvilCase.Files;

/// <summary>
/// The extension kept on a blob's stored name. Only ASCII letters and digits after the uploaded name's
/// last dot survive, at most ten of them; everything else about the name is ignored. A separator, "..",
/// a drive letter, a colon or a trailing dot in the uploaded name never reaches the stored path.
/// </summary>
internal static class BlobExtension
{
    private const int MaxLength = 10;

    public static string From(string? fileName)
    {
        var lastDot = fileName?.LastIndexOf('.') ?? -1;
        if (lastDot < 0)
            return "";

        var extension = fileName![(lastDot + 1)..];

        return extension.Length is 0 or > MaxLength || !extension.All(char.IsAsciiLetterOrDigit)
            ? ""
            : "." + extension;
    }
}
