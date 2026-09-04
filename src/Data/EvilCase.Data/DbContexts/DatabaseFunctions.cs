namespace EvilBrains.EvilCase.Data.DbContexts;

/// <summary>
/// Each function must also be mapped in ApplicationDbContext.
/// </summary>
public static class DatabaseFunctions
{
    /// <summary>
    /// Folds diacritics through the IMMUTABLE wrapper the <c>Init</c> migration creates.
    /// </summary>
    public static string Unaccent(string value)
    {
        throw new NotSupportedException($"{nameof(Unaccent)} runs in the database and has no in-memory form.");
    }
}
