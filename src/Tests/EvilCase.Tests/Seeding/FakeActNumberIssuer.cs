using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeActNumberIssuer : IActNumberIssuer
{
    private int issued;

    public async Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken token)
    {
        return @case.CaseNumber + "/" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + (++this.issued).ToString("000", CultureInfo.InvariantCulture);
    }
}
