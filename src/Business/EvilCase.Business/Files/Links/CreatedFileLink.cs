namespace EvilBrains.EvilCase.Business.Files.Links;

public sealed record CreatedFileLink
{
    /// <summary>
    /// Shown once; only its hash is stored.
    /// </summary>
    public required string Secret { get; init; }

    public required DateTime Expires { get; init; }
}
