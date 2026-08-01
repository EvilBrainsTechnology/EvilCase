namespace EvilBrains.EvilCase.Auth;

public enum LoginStatus
{
    Success = 0,

    /// <summary>
    /// Unknown e-mail or wrong password. The caller must not be told which.
    /// </summary>
    InvalidCredentials = 1,

    /// <summary>
    /// Too many consecutive failures. Only ever reported to someone who already caused them, so it
    /// tells an attacker nothing they did not already know.
    /// </summary>
    LockedOut = 2,
}
