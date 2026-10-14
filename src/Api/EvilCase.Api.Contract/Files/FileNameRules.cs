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
}
