namespace EvilBrains.EvilCase.Business.Files;

public enum FileVersionUploadOutcome
{
    VersionAdded = 0,

    Unchanged = 1,

    FileNotFound = 2,

    ExtensionMismatch = 3,
}
