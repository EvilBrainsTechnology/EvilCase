namespace EvilBrains.EvilCase.Business.Cases;

internal sealed record CaseSettings
{
    /// <summary>
    /// The series a case's internal file mark is generated from. <see cref="CaseReferenceSeries"/>
    /// describes the tokens.
    /// </summary>
    public required string ReferenceFormat { get; init; }
}
