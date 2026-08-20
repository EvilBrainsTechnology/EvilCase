namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class TenantScope(Action restore) : IDisposable
{
    public void Dispose() => restore();
}
