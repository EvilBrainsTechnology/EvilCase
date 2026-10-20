using EvilBrains.EvilCase.App.Auth;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// The interface is internal, so NSubstitute cannot stand in for it.
/// </summary>
internal sealed class StubEarlyRefresh(EarlyRefreshResult? result = null) : IEarlyRefresh
{
    public Task<EarlyRefreshResult?> Take()
    {
        return Task.FromResult(result);
    }
}
