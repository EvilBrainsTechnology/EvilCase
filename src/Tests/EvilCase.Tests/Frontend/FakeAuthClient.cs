using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class FakeAuthClient : IAuthClient
{
    private readonly LoginResponse? renewal;

    private readonly Exception? refreshFailure;

    public FakeAuthClient(LoginResponse renewal)
    {
        this.renewal = renewal;
    }

    public FakeAuthClient(Exception refreshFailure)
    {
        this.refreshFailure = refreshFailure;
    }

    public int Refreshes { get; private set; }

    public Task<LoginResponse> Login(LoginRequest request, CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task<LoginResponse> Refresh(CancellationToken token)
    {
        this.Refreshes++;

        return this.refreshFailure is { } failure
            ? Task.FromException<LoginResponse>(failure)
            : Task.FromResult(this.renewal!);
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
