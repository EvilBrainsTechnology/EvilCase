namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// What the sign-in page has to be able to say. Every value maps to one status code the API answers
/// with, so nothing has to be read out of a message.
/// </summary>
internal enum SignInOutcome
{
    Success = 0,

    InvalidCredentials = 1,

    LockedOut = 2,

    TooManyAttempts = 3,

    Unreachable = 4,
}
