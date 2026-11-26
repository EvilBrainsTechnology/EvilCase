namespace EvilBrains.EvilCase.Business.Files.Text;

public enum FileTextFailure
{
    /// <summary>
    /// The file or the version is unknown, or its blob is gone.
    /// </summary>
    FileNotFound = 0,

    /// <summary>
    /// The format is neither a text format, PDF nor DOCX.
    /// </summary>
    UnsupportedFormat = 1,

    TooLarge = 2,

    /// <summary>
    /// The format carries text, this file none: a scan, or an empty document.
    /// </summary>
    NoText = 3,

    /// <summary>
    /// The content does not parse as its extension says.
    /// </summary>
    Unreadable = 4,
}
