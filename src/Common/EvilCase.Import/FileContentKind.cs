namespace EvilBrains.EvilCase.Import;

/// <summary>
/// What a file is, decided from its bytes. This is the format and nothing more — the role a file plays
/// in an act (source, final, attachment, envelope) is not in here, because a final decision and its
/// attachment are both <see cref="Pdf"/>.
/// </summary>
public enum FileContentKind
{
    /// <summary>
    /// Nothing recognised it. A file this classifier cannot place is still imported; it is not an error.
    /// </summary>
    Unknown = 0,

    Pdf = 1,

    /// <summary>
    /// Office Open XML word processing — a zip carrying a <c>word/</c> part.
    /// </summary>
    WordDocument = 2,

    /// <summary>
    /// A zip that is not a word document.
    /// </summary>
    Zip = 3,

    /// <summary>
    /// An XML declaration at the start. A data-box envelope is one of these; telling it from any other
    /// XML needs the envelope's root element, which is not yet pinned down.
    /// </summary>
    Xml = 4,
}
