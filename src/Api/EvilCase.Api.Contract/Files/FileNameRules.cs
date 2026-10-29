namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The name a stored file carries, whether it arrives with an upload or a rename (SDD-012).
/// </summary>
public static class FileNameRules
{
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
}
