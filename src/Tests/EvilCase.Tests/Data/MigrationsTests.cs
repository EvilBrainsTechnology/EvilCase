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
                Assert.That(columns, Is.SupersetOf(table.Columns.Select(column => column.Name)), $"the migrations leave {table.Name} without every column the model maps");
            }
        }
    }

    private static Dictionary<string, HashSet<string>> Replay(DbContext context)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        var provider = context.GetService<IDatabaseProvider>().Name;
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        var operations = assembly.Migrations
            .Select(entry => assembly.CreateMigration(entry.Value, provider))
            .SelectMany(migration => migration.UpOperations);

        foreach (var operation in operations)
            Apply(tables, operation);

        return tables;
    }

    private static void Apply(Dictionary<string, HashSet<string>> tables, MigrationOperation operation)
    {
        if (operation is CreateTableOperation create)
            tables[create.Name] = new HashSet<string>(create.Columns.Select(column => column.Name), StringComparer.Ordinal);
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
}
