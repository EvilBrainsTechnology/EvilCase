using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeActNumberIssuer : IActNumberIssuer
{
    private int issued;

    public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken)
    {
        return Task.FromResult(@case.CaseNumber + "/" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + (++this.issued).ToString("000", CultureInfo.InvariantCulture));
    }
}
