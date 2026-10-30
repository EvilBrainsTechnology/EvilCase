namespace EvilBrains.EvilCase.Business.Files.Links;

public sealed record FileLinkCreateResult
{
    public required FileLinkCreateOutcome Outcome { get; init; }

    /// <summary>
    /// Shown once; only its hash is stored.
    /// </summary>
    public string? Secret { get; init; }

    public DateTime? Expires { get; init; }
}
