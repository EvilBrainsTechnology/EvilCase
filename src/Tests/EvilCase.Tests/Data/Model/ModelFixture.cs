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
public abstract class ModelFixture
{
    private static readonly (IReadOnlyModel Runtime, IReadOnlyModel DesignTime) Models = Build();

    protected static IReadOnlyModel Model => Models.Runtime;

    // The read-optimized model drops check constraints; only the design-time one carries them.
    protected static IReadOnlyModel DesignTimeModel => Models.DesignTime;

    protected static List<string> ColumnsOf(IReadOnlyEntityType? entityType) =>
        entityType?.GetProperties().Select(property => property.GetColumnName()).ToList() ?? [];

    protected static List<string> Naming(IEnumerable<string> names, string word) =>
        [.. names.Where(name => name.Contains(word, StringComparison.OrdinalIgnoreCase))];

    protected static IReadOnlyForeignKey? ForeignKeyTo<TPrincipal>(IReadOnlyEntityType entityType) =>
        entityType.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(TPrincipal));

    protected static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));

    private static (IReadOnlyModel, IReadOnlyModel) Build()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);
        return (context.Model, context.GetService<IDesignTimeModel>().Model);
    }
}
