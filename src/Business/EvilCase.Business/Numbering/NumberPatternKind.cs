namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Which of the two patterns is being read. Only an act number has a case to name.
/// </summary>
internal enum NumberPatternKind
{
    CaseNumber = 0,

    ActNumber = 1,
}
