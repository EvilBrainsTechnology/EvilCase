namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The signers of a stored signed file (p7m, p7s), verified where the verification answers.
/// </summary>
public sealed record FileSignersResponse
{
    /// <summary>
    /// Set where the file is not read: no signers then.
    /// </summary>
    public FileSignersProblem? Problem { get; init; }

    public required IReadOnlyList<FileSigner> Signers { get; init; }

    /// <summary>
    /// For a detached signature, the file it signs.
    /// </summary>
    public FileListItem? SignedFile { get; init; }

    /// <summary>
    /// For a detached signature without the file it signs.
    /// </summary>
    public DetachedSignatureProblem? SignedFileProblem { get; init; }
}
