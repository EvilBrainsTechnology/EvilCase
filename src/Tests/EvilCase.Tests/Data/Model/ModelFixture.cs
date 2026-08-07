using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

/// <summary>
/// Builds the model without touching a server — the design-time factory names no connection string,
/// and nothing here opens one. What it pins are the conventions in
/// <c>.claude/rules/data.md</c>, which a new entity is otherwise free to forget silently. The
/// read-optimized <see cref="Model"/> has dropped check constraints; <see cref="DesignTimeModel"/>
/// carries them.
/// </summary>
internal static class ModelFixture
{
    static ModelFixture()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);
        Model = context.Model;
        DesignTimeModel = context.GetService<IDesignTimeModel>().Model;
    }

    public static IModel Model { get; }

    public static IModel DesignTimeModel { get; }

    public static List<string> ColumnsOf(IReadOnlyEntityType? entityType) =>
        entityType?.GetProperties().Select(property => property.GetColumnName()).ToList() ?? [];

    public static List<string> Naming(IEnumerable<string> names, string word) =>
        [.. names.Where(name => name.Contains(word, StringComparison.OrdinalIgnoreCase))];

    public static IReadOnlyForeignKey? ForeignKeyTo<TPrincipal>(IReadOnlyEntityType entityType) =>
        entityType.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(TPrincipal));

    public static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));
}
