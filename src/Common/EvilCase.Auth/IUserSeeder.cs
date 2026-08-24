namespace EvilBrains.EvilCase.Auth;

internal interface IUserSeeder
{
    /// <summary>
    /// Whether the configuration names both a seed e-mail and a seed password. False leaves the
    /// database untouched, the connection included.
    /// </summary>
    public bool IsConfigured { get; }

    /// <summary>
    /// Creates the first administrator from configuration. Does nothing where no seed credentials are
    /// configured, and nothing at all once any user exists — it never overwrites.
    /// </summary>
    public Task SeedUser(CancellationToken token);
}
