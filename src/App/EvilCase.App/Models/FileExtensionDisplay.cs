namespace EvilBrains.EvilCase.App.Models;

public static class FileExtensionDisplay
{
    private const int MaxLength = 4;

    /// <summary>
    /// The short label on a file row's icon: the extension, uppercase, at most 4 characters.
    /// </summary>
    public static string Text(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');

        if (extension.Length == 0)
            return "FILE";

        return extension.Length > MaxLength
            ? extension[..MaxLength].ToUpperInvariant()
            : extension.ToUpperInvariant();
    }
}
