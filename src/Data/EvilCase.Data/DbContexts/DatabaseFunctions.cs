namespace EvilBrains.EvilCase.Data.DbContexts;

/// <summary>
/// Functions the database carries. Each one is mapped in <see cref="ApplicationDbContext"/> and has
/// meaning only inside a query.
/// </summary>
public static class DatabaseFunctions
{
    /// <summary>
    /// Folds diacritics through the IMMUTABLE wrapper the <c>Init</c> migration creates (SDD-014).
    /// </summary>
    public static string Unaccent(string value) =>
        throw new NotSupportedException($"{nameof(Unaccent)} runs in the database and has no in-memory form.");
}
