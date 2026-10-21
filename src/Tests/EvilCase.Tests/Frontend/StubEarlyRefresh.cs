using EvilBrains.EvilCase.App.Auth;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// The interface is internal, so NSubstitute cannot stand in for it.
/// </summary>
internal sealed class StubEarlyRefresh(EarlyRefreshResult? early = null, EarlyRefreshResult? refreshed = null) : IEarlyRefresh
{
    public int Refreshes { get; private set; }

    public static EarlyRefreshResult Renewed(string email)
    {
        return new(200, $$"""{"expiresAt":"{{DateTime.UtcNow.AddMinutes(10):O}}","email":"{{email}}","role":"User"}""");
    }

    public Task<EarlyRefreshResult?> Take()
    {
        return Task.FromResult(early);
    }

    public Task<EarlyRefreshResult?> Refresh(CancellationToken token)
    {
        this.Refreshes++;

        return Task.FromResult(refreshed);
    }
}
