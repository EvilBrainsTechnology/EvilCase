namespace EvilBrains.EvilCase.Auth;

public enum RefreshStatus
{
    Success = 0,

    /// <summary>
    /// Unknown, expired, revoked past the grace window, or belonging to a locked-out account. Whatever
    /// the browser is holding is worthless and has to go with the refusal.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// Spent moments ago by another tab of the same browser. Refused like any other spent token, but the
    /// cookie already holds the replacement — taking it away would end the session that tab just renewed.
    /// </summary>
    Raced = 2,
}
