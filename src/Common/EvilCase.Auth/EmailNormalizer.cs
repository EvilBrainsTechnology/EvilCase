namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Lower-cased before the unique index, so no query folds case in the database.
/// </summary>
internal static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
