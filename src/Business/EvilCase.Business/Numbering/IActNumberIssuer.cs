using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

internal interface IActNumberIssuer
{
    /// <summary>
    /// The next free act number of the day inside the case. The caller saves the act; a collision with a
    /// number issued at the same moment is not handled yet and comes with the writer (M4).
    /// </summary>
    public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken = default);
}
