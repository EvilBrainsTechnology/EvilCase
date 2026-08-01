namespace EvilBrains.EvilCase.Auth;

public sealed record LoginResult
{
    public required LoginStatus Status { get; init; }

    /// <summary>
    /// Set exactly when <see cref="Status"/> is <see cref="LoginStatus.Success"/>.
    /// </summary>
    public AuthSession? Session { get; init; }

    public static LoginResult Succeeded(AuthSession session) =>
        new() { Status = LoginStatus.Success, Session = session };

    public static LoginResult Failed(LoginStatus status) => new() { Status = status };
}
