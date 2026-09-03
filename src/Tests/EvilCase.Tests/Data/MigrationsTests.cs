using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Replays what the migrations do and compares it with the model. A migration whose <c>Up</c> is empty
/// still leaves the snapshot agreeing with the model, so this is the only thing that sees a table the
/// database never gets.
/// </summary>
public class MigrationsTests
{
    [Test]
    public void TheMigrationsBuildEveryTableTheModelMaps()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var migrated = Replay(context);
        var mapped = context.Model.GetRelationalModel().Tables;

        Assert.That(mapped, Is.Not.Empty, "the model maps tables at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var table in mapped)
            {
                var columns = migrated.GetValueOrDefault(table.Name) ?? [];

                Assert.That(migrated.Keys, Does.Contain(table.Name), $"no migration creates {table.Name}");
                Assert.That(columns, Is.SupersetOf(table.Columns.Select(static column => column.Name)), $"the migrations leave {table.Name} without every column the model maps");
            }
        }
    }

    [Test]
    public void TheMigrationsCreateEveryIndexTheModelMaps()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var created = ReplayIndexes(context);
        var mapped = context.Model.GetRelationalModel().Tables
            .SelectMany(static table => table.Indexes)
            .Select(static index => index.Name)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped, Is.Not.Empty, "the model maps indexes at all, or this test passes vacuously");
            Assert.That(created, Is.EquivalentTo(mapped), "the migrations and the model disagree about indexes");
        }
    }

    [Test]
    public void TheSnapshotAgreesWithTheModel()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        Assert.That(context.Database.HasPendingModelChanges(), Is.False, "the migration and its snapshot are behind the model, so a fresh database is not the one the model maps");
    }

    private static Dictionary<string, HashSet<string>> Replay(DbContext context)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        var provider = context.GetService<IDatabaseProvider>().Name;
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        var operations = assembly.Migrations
            .Select(entry => assembly.CreateMigration(entry.Value, provider))
            .SelectMany(static migration => migration.UpOperations);

        foreach (var operation in operations)
            Apply(tables, operation);

        return tables;
    }

    private static void Apply(Dictionary<string, HashSet<string>> tables, MigrationOperation operation)
    {
        if (operation is CreateTableOperation create)
            tables[create.Name] = new HashSet<string>(create.Columns.Select(static column => column.Name), StringComparer.Ordinal);
        else if (operation is AddColumnOperation add)
            tables[add.Table].Add(add.Name);
        else if (operation is DropColumnOperation dropColumn)
            tables[dropColumn.Table].Remove(dropColumn.Name);
        else if (operation is DropTableOperation dropTable)
            tables.Remove(dropTable.Name);
        else if (operation is RenameTableOperation renameTable)
            Rename(tables, renameTable);
        else if (operation is RenameColumnOperation renameColumn)
            Rename(tables[renameColumn.Table], renameColumn);
    }

    private static void Rename(Dictionary<string, HashSet<string>> tables, RenameTableOperation operation)
    {
        if (operation.NewName is null || !tables.Remove(operation.Name, out var columns))
            return;

        tables[operation.NewName] = columns;
    }

    private static void Rename(HashSet<string> columns, RenameColumnOperation operation)
    {
        columns.Remove(operation.Name);
        columns.Add(operation.NewName);
    }

    /// <summary>
    /// Index names left standing once every drop is replayed: a dropped table takes its indexes with it.
    /// </summary>
    private static List<string> ReplayIndexes(DbContext context)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        var provider = context.GetService<IDatabaseProvider>().Name;
        var indexes = new Dictionary<string, string>(StringComparer.Ordinal);

        var operations = assembly.Migrations
            .Select(entry => assembly.CreateMigration(entry.Value, provider))
            .SelectMany(static migration => migration.UpOperations);

        foreach (var operation in operations)
        {
            if (operation is CreateIndexOperation create)
            {
                indexes[create.Name] = create.Table;
            }
            else if (operation is DropIndexOperation drop)
            {
                indexes.Remove(drop.Name);
            }
            else if (operation is DropTableOperation dropTable)
            {
                var dropped = indexes
                    .Where(entry => string.Equals(entry.Value, dropTable.Name, StringComparison.Ordinal))
                    .Select(static entry => entry.Key)
                    .ToList();

                foreach (var name in dropped)
                    indexes.Remove(name);
            }
        }

        return [.. indexes.Keys];
    }
}
