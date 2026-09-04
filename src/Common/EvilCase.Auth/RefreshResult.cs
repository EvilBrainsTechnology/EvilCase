namespace EvilBrains.EvilCase.Auth;

public sealed record RefreshResult
{
    public required RefreshStatus Status { get; init; }

    public AuthSession? Session { get; init; }

    public static RefreshResult Succeeded(AuthSession session)
    {
        return new() { Status = RefreshStatus.Success, Session = session };
    }

    public static RefreshResult Failed(RefreshStatus status)
    {
        return new() { Status = status };
    }
}
