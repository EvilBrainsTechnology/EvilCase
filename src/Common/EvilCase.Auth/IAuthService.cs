namespace EvilBrains.EvilCase.Auth;

public interface IAuthService
{
    public Task<LoginResult> Login(string email, string password, ClientInfo client, CancellationToken token);

    public Task<RefreshResult> Refresh(string refreshToken, ClientInfo client, CancellationToken token);

    public Task SignOut(string refreshToken, CancellationToken token);

    public Task SignOutEverywhere(Guid userId, CancellationToken token);

    public Task<IReadOnlyList<UserSession>> GetSessions(Guid userId, CancellationToken token);
}
