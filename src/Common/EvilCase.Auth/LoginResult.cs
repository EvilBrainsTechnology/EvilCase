namespace EvilBrains.EvilCase.Auth;

public sealed record LoginResult
{
    public required LoginStatus Status { get; init; }

    public AuthSession? Session { get; init; }

    public static LoginResult Succeeded(AuthSession session)
    {
        return new() { Status = LoginStatus.Success, Session = session };
    }

    public static LoginResult Failed(LoginStatus status)
    {
        return new() { Status = status };
    }
}
