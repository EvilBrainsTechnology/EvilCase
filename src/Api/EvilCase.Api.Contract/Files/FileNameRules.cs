namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The name a stored file carries, whether it arrives with an upload or a rename (SDD-012).
/// </summary>
public static class FileNameRules
{
    private const string InvalidNameCharacters = "<>:\"|?*/\\";

    public static string LastSegment(string fileName)
    {
        return Path.GetFileName(fileName);
    }

    public static string? Violation(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "The file name must not be empty.";

        return fileName.Length > FileLimits.MaxFileNameLength
            ? $"The file name must not exceed {FileLimits.MaxFileNameLength} characters."
            : null;
    }

    /// <summary>
    /// Whether a new version keeps the file's extension, ignoring case (SDD-012).
    /// </summary>
    public static bool SameExtension(string fileName, string otherFileName)
    {
        return string.Equals(Path.GetExtension(fileName), Path.GetExtension(otherFileName), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The name a file is saved under: what a file name cannot carry, path separators included, becomes an
    /// underscore, and spaces and dots at either end go; the fallback where nothing remains.
    /// </summary>
    public static string SafeFileName(string fileName, string fallback)
    {
        var safe = new string([.. fileName.Select(static character => IsInvalidInName(character) ? '_' : character)]).Trim(' ', '.');

        return safe.Length == 0 ? fallback : safe;
    }

    private static bool IsInvalidInName(char character)
    {
        // A format character such as a right-to-left override would disguise the extension.
        return char.IsControl(character)
            || char.GetUnicodeCategory(character) == UnicodeCategory.Format
            || InvalidNameCharacters.Contains(character, StringComparison.Ordinal);
    }
}
