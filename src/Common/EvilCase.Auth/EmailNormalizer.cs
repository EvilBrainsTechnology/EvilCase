namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// E-mails are stored and looked up in one canonical form, so the unique index is what makes them
/// case-insensitive and no query has to fold case in the database.
/// </summary>
internal static class EmailNormalizer
{
    // CA1308 wants uppercase, which is right where the normalized value is a security decision separate
    // from what the user sees. Here it is the stored value and the displayed one at once, and lower case
    // is the convention for e-mail — an address is never shown back shouting.
#pragma warning disable CA1308
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
#pragma warning restore CA1308
}
