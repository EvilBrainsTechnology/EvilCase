namespace EvilBrains.EvilCase.Auth;

internal interface IUserSeeder
{
    /// <summary>
    /// Creates the first administrator from configuration. Does nothing where no seed credentials are
    /// configured, and nothing at all once any user exists — it never overwrites.
    /// </summary>
    public Task Seed(CancellationToken cancellationToken);
}
