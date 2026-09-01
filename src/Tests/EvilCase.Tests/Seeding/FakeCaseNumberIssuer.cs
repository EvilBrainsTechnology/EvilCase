using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeCaseNumberIssuer : ICaseNumberIssuer
{
    private int issued;

    public async Task<string> NextCaseNumber(DateOnly date, CancellationToken token)
    {
        return await Task.FromResult("EC/" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + (++this.issued).ToString("000", CultureInfo.InvariantCulture));
    }
}
