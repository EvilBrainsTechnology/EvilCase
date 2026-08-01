namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// E-mails are stored and looked up in one canonical form, so the unique index is what makes them
/// case-insensitive and no query has to fold case in the database.
/// </summary>
internal static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
