using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Hands out an act's own number and defends it against a race (SDD-008). The caller builds the act
/// itself and saves it through <see cref="Save"/>.
/// </summary>
public interface IActNumberIssuer
{
    /// <summary>The day's next free act number under the case.</summary>
    public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves what the session tracks. A race that took the number re-issues the next sequence of the same
    /// day and saves again; a hand-written number is never renumbered.
    /// </summary>
    public Task Save(Act act, CancellationToken cancellationToken = default);
}
