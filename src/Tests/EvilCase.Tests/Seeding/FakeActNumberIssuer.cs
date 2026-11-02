using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeActNumberIssuer : IActNumberIssuer
{
    private int issued;

    public async Task<string> NextActNumber(string caseNumber, DateOnly date, CancellationToken token)
    {
        return caseNumber + "/" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + (++this.issued).ToString("000", CultureInfo.InvariantCulture);
    }
}
