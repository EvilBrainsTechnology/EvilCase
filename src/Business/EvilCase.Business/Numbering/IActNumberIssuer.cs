using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

internal interface IActNumberIssuer
{
    /// <summary>
    /// The next free act number of the day inside the case.
    /// </summary>
    public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken);
}
