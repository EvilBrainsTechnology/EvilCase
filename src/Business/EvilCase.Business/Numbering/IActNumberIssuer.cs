using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

internal interface IActNumberIssuer
{
    public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken token);
}
