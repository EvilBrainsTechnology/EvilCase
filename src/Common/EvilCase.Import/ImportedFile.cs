namespace EvilBrains.EvilCase.Import;

/// <summary>
/// One file placed under an act by its name. What the file <em>is</em> comes from its bytes
/// (<see cref="FileContentClassifier"/>) and is deliberately not here.
/// </summary>
public sealed record ImportedFile
{
    /// <summary>
    /// The file name exactly as it was on disk.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// True when the ordinal carried a letter — <c>05a</c> is an attachment of act five, <c>05</c> is
    /// not.
    /// </summary>
    public required bool IsAttachment { get; init; }

    /// <summary>
    /// Everything after the ordinal, without the extension. Whatever word the convention uses for an
    /// attachment is part of this and is never matched on.
    /// </summary>
    public required string Title { get; init; }
}
