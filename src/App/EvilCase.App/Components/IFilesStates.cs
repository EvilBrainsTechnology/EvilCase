namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// Builds the files state of an act.
/// </summary>
internal interface IFilesStates
{
    public FilesCardState ForAct(Guid actId);
}
