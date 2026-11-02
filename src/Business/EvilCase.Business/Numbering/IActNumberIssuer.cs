namespace EvilBrains.EvilCase.Business.Numbering;

internal interface IActNumberIssuer
{
    public Task<string> NextActNumber(string caseNumber, DateOnly date, CancellationToken token);
}
