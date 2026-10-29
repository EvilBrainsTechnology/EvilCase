namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// What an upload stored.
/// </summary>
public enum FileUploadOutcome
{
    /// <summary>
    /// A file that did not exist before.
    /// </summary>
    NewFile = 0,

    /// <summary>
    /// A new version of an existing file.
    /// </summary>
    NewVersion = 1,

    /// <summary>
    /// Nothing: the content equals the file's last version.
    /// </summary>
    Unchanged = 2,
}
