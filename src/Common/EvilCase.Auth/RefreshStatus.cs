namespace EvilBrains.EvilCase.Auth;

public enum RefreshStatus
{
    Success = 0,

    /// <summary>
    /// The cookie is dead and must be cleared.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// Spent moments ago by another tab of the same browser. The cookie already holds the replacement —
    /// taking it away would end the session that tab just renewed.
    /// </summary>
    Raced = 2,
}
