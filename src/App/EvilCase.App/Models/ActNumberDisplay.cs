namespace EvilBrains.EvilCase.App.Models;

public static class ActNumberDisplay
{
    /// <summary>
    /// The act's own part of its number inside its case: "20250902-001" of
    /// "EC/20250528-001/20250902-001". A number without the case's prefix shows whole.
    /// </summary>
    public static string WithinCase(string actNumber, string caseNumber)
    {
        var prefix = caseNumber + "/";

        return actNumber.Length > prefix.Length && actNumber.StartsWith(prefix, StringComparison.Ordinal)
            ? actNumber[prefix.Length..]
            : actNumber;
    }
}
