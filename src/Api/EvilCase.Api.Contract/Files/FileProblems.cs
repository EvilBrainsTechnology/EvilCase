namespace EvilBrains.EvilCase.Api.Contract.Files;

public static class FileProblems
{
    public const string NotFound = "File not found";

    public const string NameTaken = "File name taken";

    public const string VersionNotFound = "File version not found";

    public const string LastVersion = "Last file version";

    public const string LinkNotFound = "File link not found";

    public const string LastVersionDetail = "The only version of a file is deleted only with the file.";

    public static string NameTakenDetail(string ownerWord)
    {
        return $"Another file of the {ownerWord} already carries the name.";
    }
}
