using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeCaseNumberIssuer : ICaseNumberIssuer
{
    private int issued;

    public Task<string> NextCaseNumber(DateOnly date, CancellationToken token)
    {
        return Task.FromResult("EC/" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + (++this.issued).ToString("000", CultureInfo.InvariantCulture));
    }
}
