namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Brings the database schema up to the migrations the running build ships with.
/// </summary>
public interface IDatabaseMigrator
{
    public Task Migrate(CancellationToken token);
}
