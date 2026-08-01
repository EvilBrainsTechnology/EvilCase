namespace EvilBrains.EvilCase.Auth;

public sealed record RefreshResult
{
    public required RefreshStatus Status { get; init; }

    /// <summary>
    /// Set exactly when <see cref="Status"/> is <see cref="RefreshStatus.Success"/>.
    /// </summary>
    public AuthSession? Session { get; init; }

    public static RefreshResult Succeeded(AuthSession session) =>
        new() { Status = RefreshStatus.Success, Session = session };

    public static RefreshResult Failed(RefreshStatus status) => new() { Status = status };
}
