using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Hands out a case's own number and defends it against a race (SDD-008). The caller builds the case
/// itself and saves it through <see cref="Save"/>.
/// </summary>
public interface ICaseNumberIssuer
{
    /// <summary>
    /// The day's next free case number.
    /// </summary>
    public Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves what the session tracks. A race that took the number re-issues the next sequence of the same
    /// day and saves again; a hand-written number is never renumbered.
    /// </summary>
    public Task Save(Case @case, CancellationToken cancellationToken = default);
}
