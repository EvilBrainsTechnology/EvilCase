namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// Builds the files state of a case or an act.
/// </summary>
internal interface IFilesStates
{
    public FilesCardState ForCase(Guid caseId);

    public FilesCardState ForAct(Guid actId);
}
