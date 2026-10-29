namespace EvilBrains.EvilCase.Business.Files;

public enum UploadFileOutcome
{
    /// <summary>
    /// A new file.
    /// </summary>
    Uploaded = 0,

    OwnerNotFound = 1,

    VersionAdded = 2,

    Unchanged = 3,
}
