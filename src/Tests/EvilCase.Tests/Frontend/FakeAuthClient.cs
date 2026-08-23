using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// An <see cref="IAuthClient"/> whose only interesting call is <see cref="Refresh"/>; everything else
/// is unreachable from the tests that use it.
/// </summary>
internal sealed class FakeAuthClient(Exception refreshFailure) : IAuthClient
{
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task<LoginResponse> Refresh(CancellationToken token)
    {
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
