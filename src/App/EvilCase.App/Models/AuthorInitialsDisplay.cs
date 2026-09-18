namespace EvilBrains.EvilCase.App.Models;

public static class AuthorInitialsDisplay
{
    /// <summary>
    /// The one or two letters on a comment's avatar; User carries no name but its e-mail.
    /// </summary>
    public static string Text(string email)
    {
        var local = email.Split('@')[0];
        var letters = local
            .Split(['.', '_', '-', '+'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => part[0])
            .Where(static letter => char.IsLetterOrDigit(letter))
            .Take(2)
            .ToArray();

        return letters.Length == 0 ? "?" : new string(letters).ToUpperInvariant();
    }
}
