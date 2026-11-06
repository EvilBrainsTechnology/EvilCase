namespace EvilBrains.EvilCase.Business.Files;

public enum FileVersionUploadOutcome
{
    VersionAdded = 0,

    Unchanged = 1,

    FileNotFound = 2,

    ExtensionMismatch = 3,

    /// <summary>
    /// The version to restore is unknown or has no blob.
    /// </summary>
    VersionNotFound = 4,
}
