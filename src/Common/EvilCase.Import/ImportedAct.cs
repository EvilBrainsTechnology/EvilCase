namespace EvilBrains.EvilCase.Import;

/// <summary>
/// The files of one ordinal, gathered. One act is one number in the enclosing folder, however many
/// files carry it.
/// </summary>
public sealed record ImportedAct
{
    public required int Ordinal { get; init; }

    /// <summary>
    /// Taken from the first file of the act that is not an attachment, because an attachment's name
    /// describes the attachment rather than the act.
    /// </summary>
    public required string Title { get; init; }

    public IReadOnlyList<ImportedFile> Files { get; init; } = [];
}
