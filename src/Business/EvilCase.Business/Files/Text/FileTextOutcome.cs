namespace EvilBrains.EvilCase.Business.Files.Text;

public enum FileTextOutcome
{
    Read = 0,

    /// <summary>
    /// The file or the version is unknown, or its blob is gone.
    /// </summary>
    FileNotFound = 1,

    /// <summary>
    /// The format is neither a text format, PDF nor DOCX.
    /// </summary>
    UnsupportedFormat = 2,

    TooLarge = 3,

    /// <summary>
    /// The format carries text, this file none: a scan, or an empty document.
    /// </summary>
    NoText = 4,

    /// <summary>
    /// The content does not parse as its extension says.
    /// </summary>
    Unreadable = 5,

    PageNotFound = 6,
}
