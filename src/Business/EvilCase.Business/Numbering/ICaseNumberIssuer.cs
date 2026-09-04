namespace EvilBrains.EvilCase.Business.Numbering;

internal interface ICaseNumberIssuer
{
    public Task<string> NextCaseNumber(DateOnly date, CancellationToken token);
}
