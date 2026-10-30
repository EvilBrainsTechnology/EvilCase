namespace EvilBrains.EvilCase.Business.Files.Links;

public sealed record FileLinkConsumeResult
{
    public required FileLinkConsumeOutcome Outcome { get; init; }

    /// <summary>
    /// Set where the outcome is <see cref="FileLinkConsumeOutcome.Consumed"/>.
    /// </summary>
    public ConsumedFileLink? Link { get; init; }
}
