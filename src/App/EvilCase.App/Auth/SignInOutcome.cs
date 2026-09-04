namespace EvilBrains.EvilCase.App.Auth;

internal enum SignInOutcome
{
    Success = 0,

    InvalidCredentials = 1,

    LockedOut = 2,

    TooManyAttempts = 3,

    Unreachable = 4,
}
