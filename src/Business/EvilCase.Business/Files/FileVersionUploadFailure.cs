namespace EvilBrains.EvilCase.Business.Files;

public enum FileVersionUploadFailure
{
    FileNotFound = 0,

    ExtensionMismatch = 1,

    /// <summary>
    /// The version to restore is unknown or has no blob.
    /// </summary>
    VersionNotFound = 2,
}
