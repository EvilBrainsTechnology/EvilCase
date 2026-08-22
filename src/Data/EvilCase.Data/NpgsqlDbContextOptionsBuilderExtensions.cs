using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace EvilBrains.EvilCase.Data;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
    // Nothing references the migrations assembly at compile time — it references this one — so it is
    // loaded by name and the host carries it into its output through a project reference.
    private const string MigrationsAssemblyName = "EvilBrains.EvilCase.Data.Migrations";

    private const string MigrationsHistoryTableName = "_MigrationsHistory";

    /// <summary>
    /// Runtime and design time must configure this identically: a mismatched history table would send
    /// EF to the default __EFMigrationsHistory, where it would find nothing and re-apply every migration.
    /// </summary>
    public static NpgsqlDbContextOptionsBuilder UseEvilCaseMigrations(this NpgsqlDbContextOptionsBuilder builder)
    {
        builder.MigrationsAssembly(MigrationsAssemblyName);
        builder.MigrationsHistoryTable(MigrationsHistoryTableName);

        return builder;
    }
}
