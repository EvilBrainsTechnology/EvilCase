namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The signers of a stored signed file (p7m, p7s).
/// </summary>
public sealed record FileSignersResponse
{
    /// <summary>
    /// Set where the file is not read: no signers then.
    /// </summary>
    public FileSignersProblem? Problem { get; init; }

    public required IReadOnlyList<FileSigner> Signers { get; init; }
}
