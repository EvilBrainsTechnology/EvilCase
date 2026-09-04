namespace EvilBrains.EvilCase.Auth;

public enum LoginStatus
{
    Success = 0,

    /// <summary>
    /// Unknown e-mail or wrong password. The caller must not be told which.
    /// </summary>
    InvalidCredentials = 1,

    /// <summary>
    /// Safe to report: only the caller who caused the failures sees it.
    /// </summary>
    LockedOut = 2,
}
