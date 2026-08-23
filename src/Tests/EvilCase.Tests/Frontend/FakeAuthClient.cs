using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class FakeAuthClient(Exception refreshFailure) : IAuthClient
{
    public int Refreshes { get; private set; }

    public Task<LoginResponse> Login(LoginRequest request, CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task<LoginResponse> Refresh(CancellationToken token)
    {
        this.Refreshes++;

        return Task.FromException<LoginResponse>(refreshFailure);
    }

    public Task Logout(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task LogoutAll(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<SessionInfo>> Sessions(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task<UserInfo> UserInfo()
    {
        throw new NotSupportedException();
    }
}
