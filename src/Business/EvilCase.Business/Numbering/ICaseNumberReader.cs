namespace EvilBrains.EvilCase.Business.Numbering;

internal interface ICaseNumberReader
{
    /// <summary>
    /// The number an act of this case is written under. Throws <see cref="Cases.CaseNotFoundException"/>
    /// when the caller owns no such case, rather than numbering an act under an empty string.
    /// </summary>
    public Task<string> Read(long caseId, CancellationToken cancellationToken = default);
}
