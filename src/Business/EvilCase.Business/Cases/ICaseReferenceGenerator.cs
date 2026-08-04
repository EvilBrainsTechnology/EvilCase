namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Produces the internal file mark a new case is created with.
/// </summary>
public interface ICaseReferenceGenerator
{
    /// <summary>
    /// The next mark in the owner's series for today. Two callers can be handed the same one; the
    /// unique index settles it and the loser retries.
    /// </summary>
    public Task<string> Next(CancellationToken cancellationToken = default);
}
