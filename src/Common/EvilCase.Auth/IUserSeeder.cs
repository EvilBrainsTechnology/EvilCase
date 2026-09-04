namespace EvilBrains.EvilCase.Auth;

internal interface IUserSeeder
{
    public bool IsConfigured { get; }

    public Task SeedUser(CancellationToken token);
}
