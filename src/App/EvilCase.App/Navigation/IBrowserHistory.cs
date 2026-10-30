namespace EvilBrains.EvilCase.App.Navigation;

/// <summary>
/// The steps the user takes through the browser's history by Back and Forward.
/// </summary>
internal interface IBrowserHistory
{
    /// <summary>
    /// Starts remembering the steps, forgetting those already taken.
    /// </summary>
    public Task TrackSteps();

    /// <summary>
    /// The distance from the current entry to the one the latest remembered step to <paramref name="target"/> reached,
    /// 0 when there is none; forgets every step.
    /// </summary>
    public Task<int> TakeStep(string target);

    /// <summary>
    /// Moves through the history by <paramref name="distance"/> entries, as Back or Forward does.
    /// </summary>
    public Task Go(int distance);
}
