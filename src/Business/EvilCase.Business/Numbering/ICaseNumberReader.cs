namespace EvilBrains.EvilCase.Business.Numbering;

internal interface ICaseNumberReader
{
    public Task<string> Read(long caseId, CancellationToken cancellationToken = default);
}
