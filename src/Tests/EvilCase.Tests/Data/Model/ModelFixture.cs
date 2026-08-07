using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

/// <summary>
/// Builds the model without touching a server — the design-time factory names no connection string,
/// and nothing here opens one. What the fixtures pin are the conventions in
/// <c>.claude/rules/data.md</c>, which a new entity is otherwise free to forget silently.
/// </summary>
internal static class ModelFixture
{
    private static readonly Lazy<(IModel Runtime, IModel DesignTime)> Built = new(Build);

    public static IModel Runtime => Built.Value.Runtime;

    // The read-optimized model drops check constraints; only the design-time one carries them.
    public static IModel DesignTime => Built.Value.DesignTime;

    public static List<string> ColumnsOf(IReadOnlyEntityType? entityType) =>
        entityType?.GetProperties().Select(property => property.GetColumnName()).ToList() ?? [];

    public static List<string> Naming(IEnumerable<string> names, string word) =>
        [.. names.Where(name => name.Contains(word, StringComparison.OrdinalIgnoreCase))];

    public static IReadOnlyForeignKey? ForeignKeyTo<TPrincipal>(IReadOnlyEntityType entityType) =>
        entityType.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(TPrincipal));

    public static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));

    private static (IModel, IModel) Build()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);
        return (context.Model, context.GetService<IDesignTimeModel>().Model);
    }
}
