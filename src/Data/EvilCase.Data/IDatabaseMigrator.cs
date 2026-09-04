namespace EvilBrains.EvilCase.Data;

public interface IDatabaseMigrator
{
    public Task Migrate(CancellationToken token);
}
