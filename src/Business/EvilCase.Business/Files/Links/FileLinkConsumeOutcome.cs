namespace EvilBrains.EvilCase.Business.Files.Links;

public enum FileLinkConsumeOutcome
{
    Consumed = 0,

    /// <summary>
    /// The link is unknown, used or expired.
    /// </summary>
    NotFound = 1,
}
